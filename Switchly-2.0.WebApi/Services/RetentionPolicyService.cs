using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Context;

namespace Switchly_2._0.WebApi.Services;

// FlagExposureEvents ve ConversionEvents tablolarını 90 günden eski satırlardan temizler.
// Volume büyüdükçe analytics sorguları yavaşlamasın diye gerekli.
// Postgres'in built-in declarative partitioning'i daha hızlı çözerdi (DROP TABLE)
// ama o migration breaking, MVP'de DELETE yeterli (gece çalışır, index üzerinden hızlı).
//
// İlk run: process başlar başlamaz değil, 5dk gecikme — startup'ı yavaşlatmayalım.
// Sonra her 24 saatte bir.
internal sealed class RetentionPolicyService(
    IServiceScopeFactory scopeFactory,
    ILogger<RetentionPolicyService> log
) : BackgroundService
{
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(90);
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, stoppingToken);

            using var timer = new PeriodicTimer(RunInterval);
            // İlk tick'i hemen at — startup'tan 5dk sonra ilk temizlik koşsun.
            await CleanupOnceAsync(stoppingToken);

            while (await timer.WaitForNextTickAsync(stoppingToken))
                await CleanupOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutdown — normal akış.
        }
    }

    private async Task CleanupOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SwitchlyDbContext>();

            var cutoff = DateTimeOffset.UtcNow - RetentionPeriod;

            // ExecuteDeleteAsync EF Core 7+ — single SQL DELETE, in-memory load yok.
            var exposureDeleted = await context.FlagExposureEvents
                .Where(e => e.OccurredAt < cutoff)
                .ExecuteDeleteAsync(ct);

            var conversionDeleted = await context.ConversionEvents
                .Where(c => c.OccurredAt < cutoff)
                .ExecuteDeleteAsync(ct);

            if (exposureDeleted > 0 || conversionDeleted > 0)
                log.LogInformation(
                    "Retention cleanup: {ExposureDeleted} exposure + {ConversionDeleted} conversion rows older than {Cutoff} deleted.",
                    exposureDeleted, conversionDeleted, cutoff);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Fail-soft: ertesi gün tekrar denenir, app'i kırma.
            log.LogWarning(ex, "Retention cleanup failed.");
        }
    }
}
