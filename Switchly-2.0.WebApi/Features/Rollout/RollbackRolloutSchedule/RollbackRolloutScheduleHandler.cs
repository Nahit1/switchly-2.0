using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.Rollout.RollbackRolloutSchedule;

public sealed record RollbackRolloutScheduleCommand(Guid ScheduleId) : IRequest<Response<bool>>;

public sealed class RollbackRolloutScheduleHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<RollbackRolloutScheduleCommand, Response<bool>>
{
    public async Task<Response<bool>> Handle(RollbackRolloutScheduleCommand request, CancellationToken ct)
    {
        var schedule = await context.RolloutSchedules
            .Include(s => s.FeatureFlagEnvironment)
                .ThenInclude(e => e.FeatureFlag)
                    .ThenInclude(f => f.Project)
            .Include(s => s.FeatureFlagEnvironment)
                .ThenInclude(e => e.VariantWeights)
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleId, ct);

        if (schedule is null)
            return Response<bool>.Fail("Schedule bulunamadı.");

        var isMember = await context.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == schedule.FeatureFlagEnvironment.FeatureFlag.Project.OrganizationId
                           && m.UserId == userContext.UserId, ct);
        if (!isMember)
            throw new UnauthorizedAccessException("Bu organization için yetkin yok.");

        // Terminal state'tense bir şey yapamayız.
        if (schedule.Status is RolloutScheduleStatus.Completed or RolloutScheduleStatus.RolledBack)
            return Response<bool>.Fail("Zaten terminal state'te.");

        if (string.IsNullOrEmpty(schedule.PreScheduleSnapshot))
            return Response<bool>.Fail("Pre-schedule snapshot yok, rollback yapılamaz.");

        // Snapshot'ı yükle.
        RolloutWeightsApplier.RestoreSnapshot(
            context,
            schedule.FeatureFlagEnvironment,
            schedule.FeatureFlagEnvironment.FeatureFlag.Type,
            schedule.PreScheduleSnapshot,
            schedule.FeatureFlagEnvironment.VariantWeights.ToList());

        schedule.Status = RolloutScheduleStatus.RolledBack;
        schedule.RolledBackReason = "manual";
        // PausedAt'i resetlemiyoruz, audit için duruyor.

        await context.SaveChangesAsync(ct);
        return Response<bool>.Ok(true);
    }
}
