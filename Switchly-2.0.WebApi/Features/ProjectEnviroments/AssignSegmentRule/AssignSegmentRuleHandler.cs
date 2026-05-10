using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.ProjectEnviroments.AssignSegmentRule;

public sealed record AssignSegmentRuleCommand(
    Guid FeatureFlagEnvironmentId,
    Guid SegmentGroupId,
    RolloutKind RolloutKind,
    int RolloutPercentage,
    int Priority
) : IRequest<Response<Guid>>;

public sealed class AssignSegmentRuleHandler(
    SwitchlyDbContext context,
    IUserContext userContext
) : IRequestHandler<AssignSegmentRuleCommand, Response<Guid>>
{
    public async Task<Response<Guid>> Handle(AssignSegmentRuleCommand request, CancellationToken ct)
    {
        // 1) Load FeatureFlagEnvironment -> OrganizationId (for auth + boundary)
        var flagEnv = await context.FeatureFlagEnvironments
            .Include(x=>x.FeatureFlag).ThenInclude(x=>x.Project)
            .AsNoTracking()
            .Where(x => x.Id == request.FeatureFlagEnvironmentId)
            .Select(x => new
            {
                x.Id,
                OrganizationId = x.FeatureFlag.Project.OrganizationId
            })
            .FirstOrDefaultAsync(ct);

        if (flagEnv is null)
            return Response<Guid>.Fail("Feature flag environment bulunamadı.");

        // 2) AuthZ: user must be a member of organization
        var isMember = await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m => m.OrganizationId == flagEnv.OrganizationId && m.UserId == userContext.UserId, ct);

        if (!isMember)
            return Response<Guid>.Fail("Bu işlem için yetkin yok.");

        // 3) SegmentGroup must belong to same organization (segments are org-scoped)
        var segmentExists = await context.SegmentGroups
            .AsNoTracking()
            .AnyAsync(sg => sg.Id == request.SegmentGroupId && sg.OrganizationId == flagEnv.OrganizationId, ct);

        if (!segmentExists)
            return Response<Guid>.Fail("Segment group bulunamadı.");

        // 4) Prevent duplicates
        var exists = await context.FeatureFlagSegmentTargetings
            .AsNoTracking()
            .AnyAsync(x => x.FeatureFlagEnvironmentId == request.FeatureFlagEnvironmentId && x.SegmentGroupId == request.SegmentGroupId, ct);

        if (exists)
            return Response<Guid>.Fail("Bu segment zaten bu environment'a atanmış.");

        // 5) Create targeting — rollout alanları request'ten, IsEnabled default true
        var pct = Math.Clamp(request.RolloutPercentage, 0, 100);
        var entity = new FeatureFlagSegmentTargeting
        {
            Id = Guid.NewGuid(),
            FeatureFlagEnvironmentId = request.FeatureFlagEnvironmentId,
            SegmentGroupId = request.SegmentGroupId,

            IsEnabled = true,
            RolloutKind = request.RolloutKind,
            RolloutPercentage = pct,
            Priority = request.Priority,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.FeatureFlagSegmentTargetings.Add(entity);
        await context.SaveChangesAsync(ct);

        return Response<Guid>.Ok(entity.Id, "Segment environment'a atandı");
    }
}