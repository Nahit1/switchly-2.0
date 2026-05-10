using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.Flags.GetRuleset;

public sealed record GetRulesetQuery(
    string PublicKey,
    string ProjectKey,
    string EnvironmentKey
) : IRequest<Response<RulesetDto>>;

public sealed record RulesetDto(
    EnvironmentRulesetDto Environment,
    List<FlagRulesetDto> Flags
);

public sealed record EnvironmentRulesetDto(Guid Id, string Key);

public sealed record FlagRulesetDto(
    Guid Id,
    string Key,
    bool EnvEnabled,
    RolloutKind DefaultRolloutKind,
    int DefaultRolloutPercentage,
    List<TargetingRulesetDto> Targetings
);

public sealed record TargetingRulesetDto(
    int Priority,
    bool IsEnabled,
    RolloutKind RolloutKind,
    int RolloutPercentage,
    List<RuleRulesetDto> Rules
);

public sealed record RuleRulesetDto(
    string TraitKey,
    string Operator,
    string? Value,
    SegmentValueType? ValueType
);

public sealed class GetRulesetHandler(SwitchlyDbContext context)
    : IRequestHandler<GetRulesetQuery, Response<RulesetDto>>
{
    public async Task<Response<RulesetDto>> Handle(GetRulesetQuery request, CancellationToken ct)
    {
        var publicKey = (request.PublicKey ?? string.Empty).Trim();
        var projectKey = (request.ProjectKey ?? string.Empty).Trim();
        var environmentKey = (request.EnvironmentKey ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(publicKey))
            return Response<RulesetDto>.Fail("PublicKey zorunludur.");
        if (string.IsNullOrWhiteSpace(projectKey))
            return Response<RulesetDto>.Fail("ProjectKey zorunludur.");
        if (string.IsNullOrWhiteSpace(environmentKey))
            return Response<RulesetDto>.Fail("EnvironmentKey zorunludur.");

        var org = await context.Organizations
            .AsNoTracking()
            .Where(o => o.PublicKey == publicKey)
            .Select(o => new { o.Id })
            .FirstOrDefaultAsync(ct);

        if (org is null)
            return Response<RulesetDto>.Fail("Organization bulunamadı.");

        var project = await context.Projects
            .AsNoTracking()
            .Where(p => p.OrganizationId == org.Id && p.Key == projectKey)
            .Select(p => new { p.Id })
            .FirstOrDefaultAsync(ct);

        if (project is null)
            return Response<RulesetDto>.Fail("Project bulunamadı.");

        var env = await context.ProjectEnvironments
            .AsNoTracking()
            .Where(e => e.ProjectId == project.Id && e.Key == environmentKey)
            .Select(e => new { e.Id, e.Key })
            .FirstOrDefaultAsync(ct);

        if (env is null)
            return Response<RulesetDto>.Fail("Environment bulunamadı.");

        // FeatureFlagEnvironments'tan başlıyoruz — IsEnabled doğrudan bu tabloda,
        // tek-aşamalı projection EF Core 9'un nested FirstOrDefault subquery'sinden
        // daha güvenilir SQL üretiyor.
        // Sadece Condition tipi rules dönülür; Group node'ları MVP eval'i tarafından ele alınmıyor.
        var envId = env.Id;
        var projectId = project.Id;

        var flags = await context.FeatureFlagEnvironments
            .AsNoTracking()
            .Where(fe => fe.ProjectEnvironmentId == envId
                         && fe.FeatureFlag.ProjectId == projectId
                         && !fe.FeatureFlag.IsArchived)
            .OrderBy(fe => fe.FeatureFlag.Key)
            .Select(fe => new FlagRulesetDto(
                fe.FeatureFlag.Id,
                fe.FeatureFlag.Key,
                fe.IsEnabled,
                fe.DefaultRolloutKind,
                fe.DefaultRolloutPercentage,
                fe.SegmentTargetings
                    .OrderByDescending(t => t.Priority)
                    .Select(t => new TargetingRulesetDto(
                        t.Priority,
                        t.IsEnabled,
                        t.RolloutKind,
                        t.RolloutPercentage,
                        t.SegmentGroup.Rules
                            .Where(r => r.NodeType == SegmentNodeType.Condition
                                        && r.TraitKey != null
                                        && r.Operator != null)
                            .OrderBy(r => r.SortOrder)
                            .Select(r => new RuleRulesetDto(
                                r.TraitKey!,
                                r.Operator!,
                                r.Value,
                                r.ValueType
                            ))
                            .ToList()
                    ))
                    .ToList()
            ))
            .ToListAsync(ct);

        return Response<RulesetDto>.Ok(new RulesetDto(
            new EnvironmentRulesetDto(env.Id, env.Key),
            flags
        ));
    }
}
