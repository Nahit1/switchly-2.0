using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.Rollout.ResumeRolloutSchedule;

public sealed record ResumeRolloutScheduleCommand(Guid ScheduleId) : IRequest<Response<bool>>;

public sealed class ResumeRolloutScheduleHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<ResumeRolloutScheduleCommand, Response<bool>>
{
    public async Task<Response<bool>> Handle(ResumeRolloutScheduleCommand request, CancellationToken ct)
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

        if (schedule.Status != RolloutScheduleStatus.Paused)
            return Response<bool>.Fail("Sadece Paused durumdaki schedule resume edilebilir.");

        // Pause süresince geçen "kayıp zamanı" affet — LastTransitionAt'ı şimdiye çek ki
        // bir sonraki promote bu adımın tam süresi kadar daha bekleyecek (fair restart).
        schedule.Status = RolloutScheduleStatus.Active;
        schedule.LastTransitionAt = DateTimeOffset.UtcNow;
        schedule.PausedAt = null;

        await context.SaveChangesAsync(ct);
        return Response<bool>.Ok(true);
    }
}
