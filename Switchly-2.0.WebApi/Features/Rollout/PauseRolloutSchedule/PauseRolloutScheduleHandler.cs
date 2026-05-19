using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.Rollout.PauseRolloutSchedule;

public sealed record PauseRolloutScheduleCommand(Guid ScheduleId) : IRequest<Response<bool>>;

public sealed class PauseRolloutScheduleHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<PauseRolloutScheduleCommand, Response<bool>>
{
    public async Task<Response<bool>> Handle(PauseRolloutScheduleCommand request, CancellationToken ct)
    {
        var schedule = await context.RolloutSchedules
            .Include(s => s.FeatureFlagEnvironment)
                .ThenInclude(e => e.FeatureFlag)
                    .ThenInclude(f => f.Project)
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleId, ct);

        if (schedule is null)
            return Response<bool>.Fail("Schedule bulunamadı.");

        var isMember = await context.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == schedule.FeatureFlagEnvironment.FeatureFlag.Project.OrganizationId
                           && m.UserId == userContext.UserId, ct);
        if (!isMember)
            throw new UnauthorizedAccessException("Bu organization için yetkin yok.");

        if (schedule.Status != RolloutScheduleStatus.Active)
            return Response<bool>.Fail("Sadece Active durumdaki schedule pause edilebilir.");

        schedule.Status = RolloutScheduleStatus.Paused;
        schedule.PausedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(ct);
        return Response<bool>.Ok(true);
    }
}
