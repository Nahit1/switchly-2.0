using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;

namespace Switchly_2._0.WebApi.Features.ProjectEnviroments.RemoveSegmentTargeting;

public sealed record RemoveSegmentTargetingCommand(Guid TargetingId)
    : IRequest<Response<string>>;

public sealed class RemoveSegmentTargetingHandler(
    SwitchlyDbContext context,
    IUserContext userContext
) : IRequestHandler<RemoveSegmentTargetingCommand, Response<string>>
{
    public async Task<Response<string>> Handle(RemoveSegmentTargetingCommand request, CancellationToken ct)
    {
        // Org boundary chain: targeting → flagEnv → flag → project → org
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

        context.FeatureFlagSegmentTargetings.Remove(targeting);
        await context.SaveChangesAsync(ct);

        return Response<string>.Ok("Segment targeting kaldırıldı.");
    }
}
