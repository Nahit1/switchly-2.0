using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Features.Rollout;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Services;

// Active schedule'lar için error guardrail kontrolü. Her dakika çalışır:
//   son ErrorWindowMinutes içinde MinSeverity üstü FlagErrorEvent sayısı >= ErrorThreshold ise
//   schedule auto-rollback olur (manuel Rollback ile aynı davranış + RolledBackReason="errors-exceeded").
internal sealed class RolloutGuardrailService(
    IServiceScopeFactory scopeFactory,
    ILogger<RolloutGuardrailService> log
) : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(45);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, stoppingToken);

            using var timer = new PeriodicTimer(TickInterval);
            await CheckOnceAsync(stoppingToken);

            while (await timer.WaitForNextTickAsync(stoppingToken))
                await CheckOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException) { /* shutdown */ }
    }

    private async Task CheckOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SwitchlyDbContext>();

            var now = DateTimeOffset.UtcNow;

            // Sadece Active + ErrorThreshold tanımlı schedule'lar (guardrail kapalıysa atla).
            var schedules = await context.RolloutSchedules
                .Include(s => s.FeatureFlagEnvironment)
                    .ThenInclude(e => e.FeatureFlag)
                .Include(s => s.FeatureFlagEnvironment)
                    .ThenInclude(e => e.VariantWeights)
                .Where(s => s.Status == RolloutScheduleStatus.Active
                            && s.ErrorThreshold != null
                            && s.ErrorThreshold > 0)
                .ToListAsync(ct);

            foreach (var schedule in schedules)
            {
                var since = now - TimeSpan.FromMinutes(schedule.ErrorWindowMinutes);
                var flagId = schedule.FeatureFlagEnvironment.FeatureFlagId;
                var envId = schedule.FeatureFlagEnvironmentId;
                var minSeverity = schedule.MinSeverity;

                // Bu flag-env'de son window içinde min severity ve üstü kaç error var?
                var count = await context.FlagErrorEvents
                    .AsNoTracking()
                    .Where(e => e.FeatureFlagId == flagId
                                && e.ProjectEnvironmentId == envId
                                && e.OccurredAt >= since
                                && (int)e.Severity >= (int)minSeverity)
                    .CountAsync(ct);

                if (count < schedule.ErrorThreshold!.Value) continue;

                // EŞIK AŞILDI → auto-rollback.
                if (string.IsNullOrEmpty(schedule.PreScheduleSnapshot))
                {
                    log.LogWarning(
                        "Schedule {ScheduleId}: error threshold exceeded ({Count}/{Threshold}) but no snapshot to restore.",
                        schedule.Id, count, schedule.ErrorThreshold);
                    continue;
                }

                try
                {
                    RolloutWeightsApplier.RestoreSnapshot(
                        context,
                        schedule.FeatureFlagEnvironment,
                        schedule.FeatureFlagEnvironment.FeatureFlag.Type,
                        schedule.PreScheduleSnapshot,
                        schedule.FeatureFlagEnvironment.VariantWeights.ToList());

                    schedule.Status = RolloutScheduleStatus.RolledBack;
                    schedule.RolledBackReason = "errors-exceeded";

                    log.LogWarning(
                        "Schedule {ScheduleId} auto-rolled back: {Count} errors in last {Window}min exceeded threshold {Threshold} (flag={FlagId}).",
                        schedule.Id, count, schedule.ErrorWindowMinutes,
                        schedule.ErrorThreshold, flagId);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Auto-rollback failed for schedule {ScheduleId}.", schedule.Id);
                }
            }

            await context.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Rollout guardrail tick failed.");
        }
    }
}
