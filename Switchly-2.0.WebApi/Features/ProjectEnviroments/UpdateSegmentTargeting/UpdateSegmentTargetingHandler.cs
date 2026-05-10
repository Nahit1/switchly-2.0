using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.ProjectEnviroments.UpdateSegmentTargeting;

public sealed record UpdateSegmentTargetingCommand(
    Guid TargetingId,
    RolloutKind RolloutKind,
    int RolloutPercentage,
    int Priority,
    bool IsEnabled
) : IRequest<Response<string>>;

public sealed class UpdateSegmentTargetingHandler(
    SwitchlyDbContext context,
    IUserContext userContext
) : IRequestHandler<UpdateSegmentTargetingCommand, Response<string>>
{
    public async Task<Response<string>> Handle(UpdateSegmentTargetingCommand request, CancellationToken ct)
    {
        // Org boundary: targeting → flagEnv → flag → project → org
        var targeting = await context.FeatureFlagSegmentTargetings
            .Include(t => t.FeatureFlagEnvironment)
                .ThenInclude(fe => fe.FeatureFlag)
                .ThenInclude(f => f.Project)
            .FirstOrDefaultAsync(t => t.Id == request.TargetingId, ct);

        if (targeting is null)
            return Response<string>.Fail("Segment targeting bulunamadı.");

        var orgId = targeting.FeatureFlagEnvironment.FeatureFlag.Project.OrganizationId;

        var isMember = await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m => m.OrganizationId == orgId && m.UserId == userContext.UserId, ct);

        if (!isMember)
            return Response<string>.Fail("Bu işlem için yetkin yok.");

        targeting.RolloutKind = request.RolloutKind;
        targeting.RolloutPercentage = Math.Clamp(request.RolloutPercentage, 0, 100);
        targeting.Priority = request.Priority;
        targeting.IsEnabled = request.IsEnabled;

        await context.SaveChangesAsync(ct);

        return Response<string>.Ok("Updated segment targeting.");
    }
}
