using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.Rollout.GetRolloutSchedule;

public sealed record GetRolloutScheduleQuery(Guid FeatureFlagEnvironmentId)
    : IRequest<Response<RolloutScheduleDto?>>;

public sealed record RolloutScheduleDto(
    Guid Id,
    Guid FeatureFlagEnvironmentId,
    Guid? TargetVariantId,
    string? TargetVariantKey,
    string Status,
    int CurrentStepIndex,
    DateTimeOffset? StartedAt,
    DateTimeOffset? LastTransitionAt,
    DateTimeOffset? PausedAt,
    DateTimeOffset? NextPromoteAt,   // hesaplanır: lastTransition + currentStep.duration
    List<RolloutScheduleStepDto> Steps
);

public sealed record RolloutScheduleStepDto(
    int StepIndex,
    int Percentage,
    int DurationMinutes,
    DateTimeOffset? PromotedAt
);

// Env'deki en güncel schedule'ı döner: aktif/paused varsa onu; yoksa en son terminal'i.
// "Hiç schedule yok" → null data (success=true).
public sealed class GetRolloutScheduleHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<GetRolloutScheduleQuery, Response<RolloutScheduleDto?>>
{
    public async Task<Response<RolloutScheduleDto?>> Handle(GetRolloutScheduleQuery request, CancellationToken ct)
    {
        var env = await context.FeatureFlagEnvironments
            .AsNoTracking()
            .Where(e => e.Id == request.FeatureFlagEnvironmentId)
            .Select(e => new { e.Id, e.FeatureFlag.Project.OrganizationId })
            .FirstOrDefaultAsync(ct);

        if (env is null)
            return Response<RolloutScheduleDto?>.Fail("Environment bulunamadı.");

        var isMember = await context.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == env.OrganizationId
                           && m.UserId == userContext.UserId, ct);
        if (!isMember)
            throw new UnauthorizedAccessException("Bu organization için yetkin yok.");

        // Aktif/paused varsa onu öncele, yoksa en yenisi.
        var schedule = await context.RolloutSchedules
            .AsNoTracking()
            .Include(s => s.Steps)
            .Include(s => s.TargetVariant)
            .Where(s => s.FeatureFlagEnvironmentId == request.FeatureFlagEnvironmentId)
            .OrderByDescending(s =>
                s.Status == RolloutScheduleStatus.Active ||
                s.Status == RolloutScheduleStatus.Paused ||
                s.Status == RolloutScheduleStatus.Draft ? 1 : 0)
            .ThenByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (schedule is null)
            return Response<RolloutScheduleDto?>.Ok(null);

        var steps = schedule.Steps
            .OrderBy(s => s.StepIndex)
            .Select(s => new RolloutScheduleStepDto(
                s.StepIndex, s.Percentage, s.DurationMinutes, s.PromotedAt))
            .ToList();

        DateTimeOffset? nextPromote = null;
        if (schedule.Status == RolloutScheduleStatus.Active && schedule.LastTransitionAt.HasValue)
        {
            var currentStep = steps.FirstOrDefault(x => x.StepIndex == schedule.CurrentStepIndex);
            // Son adımda promote yok.
            if (currentStep != null && schedule.CurrentStepIndex < steps.Count - 1)
                nextPromote = schedule.LastTransitionAt.Value.AddMinutes(currentStep.DurationMinutes);
        }

        return Response<RolloutScheduleDto?>.Ok(new RolloutScheduleDto(
            Id: schedule.Id,
            FeatureFlagEnvironmentId: schedule.FeatureFlagEnvironmentId,
            TargetVariantId: schedule.TargetVariantId,
            TargetVariantKey: schedule.TargetVariant?.Key,
            Status: schedule.Status.ToString(),
            CurrentStepIndex: schedule.CurrentStepIndex,
            StartedAt: schedule.StartedAt,
            LastTransitionAt: schedule.LastTransitionAt,
            PausedAt: schedule.PausedAt,
            NextPromoteAt: nextPromote,
            Steps: steps
        ));
    }
}
