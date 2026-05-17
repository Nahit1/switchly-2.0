using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Switchly.Sdk.Internal;

// BackgroundRefresher'ın simetriği: o periyodik olarak ruleset çekiyor,
// bu periyodik olarak queue'yu boşaltıp backend'e POST'luyor.
//
// Yapı: PeriodicTimer ile FlushInterval'da bir tick → batch drain → sender.SendAsync.
// Fail-soft: POST patladığında event'ler düşer, app çalışmaya devam.
internal sealed class BackgroundFlusher(
    IServiceScopeFactory scopeFactory,
    ExposureRecorder recorder,
    IOptions<SwitchlyOptions> options,
    ILogger<BackgroundFlusher> log
) : BackgroundService
{
    private readonly SwitchlyOptions _opts = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Tracking kapalıysa loop'a hiç girme — gereksiz timer/CPU tutmayalım.
        if (!_opts.TrackingEnabled) return;

        using var timer = new PeriodicTimer(_opts.FlushInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await FlushOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutdown — bekleniyor, sessizce çık.
        }

        // Best-effort final drain: kalan event'leri kaybetmemek için 5sn pencere.
        // Backend yavaşsa veya unreachable'sa sessizce vazgeçeriz.
        try
        {
            using var drainCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await FlushOnceAsync(drainCts.Token);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Final Switchly exposure flush during shutdown was incomplete.");
        }
    }

    private async Task FlushOnceAsync(CancellationToken ct)
    {
        var batch = DrainBatch();
        if (batch.Count == 0) return;

        try
        {
            // RulesetFetcher pattern: sender transient + named HttpClient'a bağlı,
            // her flush'ta taze scope al.
            using var scope = scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ExposureSender>();
            await sender.SendAsync(batch, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Fail-soft: event'ler kayboldu (requeue yok, MVP'de retry/backoff yok).
            // Phase 2: failed batch'i kısa bir local buffer'a koyup sonraki tick'te yeniden denemek.
            log.LogWarning(ex, "Switchly exposure batch send failed; {Count} events lost.", batch.Count);
        }
    }

    private List<ExposureEvent> DrainBatch()
    {
        var batch = new List<ExposureEvent>(_opts.FlushBatchSize);
        while (batch.Count < _opts.FlushBatchSize && recorder.Reader.TryRead(out var ev))
            batch.Add(ev);
        return batch;
    }
}
