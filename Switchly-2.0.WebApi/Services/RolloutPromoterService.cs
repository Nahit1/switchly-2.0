using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Features.Rollout;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Services;

// Aktif rollout schedule'larını dakikada bir tarar; süresi dolan adımları otomatik
// bir sonraki adıma promote eder. Son adıma ulaşılırsa Completed işaretler.
//
// MVP: tek-dakikalık tick yeterli. Daha hassas zamanlama için interval düşürülebilir.
internal sealed class RolloutPromoterService(
    IServiceScopeFactory scopeFactory,
    ILogger<RolloutPromoterService> log
) : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, stoppingToken);

            using var timer = new PeriodicTimer(TickInterval);
            await PromoteOnceAsync(stoppingToken);

            while (await timer.WaitForNextTickAsync(stoppingToken))
                await PromoteOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutdown — normal akış.
        }
    }

    private async Task PromoteOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SwitchlyDbContext>();

            var now = DateTimeOffset.UtcNow;

            // Active + LastTransition damgalı + tüm step'leri + flag/env/variants yüklü.
            var schedules = await context.RolloutSchedules
                .Include(s => s.Steps)
                .Include(s => s.FeatureFlagEnvironment)
                    .ThenInclude(e => e.FeatureFlag)
                        .ThenInclude(f => f.Variants)
                .Include(s => s.FeatureFlagEnvironment)
                    .ThenInclude(e => e.VariantWeights)
                .Where(s => s.Status == RolloutScheduleStatus.Active
                            && s.LastTransitionAt != null)
                .ToListAsync(ct);

            foreach (var schedule in schedules)
            {
                var steps = schedule.Steps.OrderBy(s => s.StepIndex).ToList();
                if (steps.Count == 0) continue;

                var currentStep = steps.FirstOrDefault(s => s.StepIndex == schedule.CurrentStepIndex);
                if (currentStep is null) continue;

                // Son adımdaysa Completed olarak işaretle.
                if (schedule.CurrentStepIndex >= steps.Count - 1)
                {
                    schedule.Status = RolloutScheduleStatus.Completed;
                    log.LogInformation(
                        "Rollout schedule {ScheduleId} completed (env={EnvId}).",
                        schedule.Id, schedule.FeatureFlagEnvironmentId);
                    continue;
                }

                // Süre doldu mu?
                var deadline = schedule.LastTransitionAt!.Value.AddMinutes(currentStep.DurationMinutes);
                if (now < deadline) continue;

                // Bir sonraki adıma geç.
                var nextIndex = schedule.CurrentStepIndex + 1;
                var nextStep = steps.FirstOrDefault(s => s.StepIndex == nextIndex);
                if (nextStep is null) continue;

                RolloutWeightsApplier.ApplyStep(
                    context,
                    schedule.FeatureFlagEnvironment,
                    schedule.FeatureFlagEnvironment.FeatureFlag.Type,
                    schedule.TargetVariantId,
                    nextStep.Percentage,
                    schedule.FeatureFlagEnvironment.VariantWeights.ToList(),
                    schedule.FeatureFlagEnvironment.FeatureFlag.Variants.ToList());

                schedule.CurrentStepIndex = nextIndex;
                schedule.LastTransitionAt = now;
                nextStep.PromotedAt = now;

                log.LogInformation(
                    "Rollout schedule {ScheduleId} promoted to step {StepIndex} ({Pct}%).",
                    schedule.Id, nextIndex, nextStep.Percentage);
            }

            await context.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Rollout promotion tick failed.");
        }
    }
}
