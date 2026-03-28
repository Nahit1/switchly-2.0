using Microsoft.EntityFrameworkCore;
using MediatR;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.Flags.EvaluateFlag;

public record EvaluateFlagRequest(
    string PublicKey,
    string ProjectKey,
    string EnvironmentKey,
    string FlagKey,
    Dictionary<string, string>? Traits
): IRequest<Response<EvaluateFlagResponseDto>>;

public record EvaluateFlagResponseDto(
    bool IsEnabled
);

public class EvaluateFlagHandler(SwitchlyDbContext context)
    : IRequestHandler<EvaluateFlagRequest, Response<EvaluateFlagResponseDto>>
{
    public async Task<Response<EvaluateFlagResponseDto>> Handle(EvaluateFlagRequest request, CancellationToken cancellationToken)
    {
        var publicKey = (request.PublicKey ?? string.Empty).Trim();
        var projectKey = (request.ProjectKey ?? string.Empty).Trim();
        var environmentKey = (request.EnvironmentKey ?? string.Empty).Trim();
        var flagKey = (request.FlagKey ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(publicKey))
            return Response<EvaluateFlagResponseDto>.Fail("PublicKey zorunludur.");

        if (string.IsNullOrWhiteSpace(projectKey))
            return Response<EvaluateFlagResponseDto>.Fail("ProjectKey zorunludur.");

        if (string.IsNullOrWhiteSpace(environmentKey))
            return Response<EvaluateFlagResponseDto>.Fail("EnvironmentKey zorunludur.");

        if (string.IsNullOrWhiteSpace(flagKey))
            return Response<EvaluateFlagResponseDto>.Fail("FlagKey zorunludur.");

        // 1) Organization
        var org = await context.Organizations
            .AsNoTracking()
            .Where(o => o.PublicKey == publicKey)
            .Select(o => new { o.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (org is null)
            return Response<EvaluateFlagResponseDto>.Fail("Organization bulunamadı.");

        // 2) Project
        var project = await context.Projects
            .AsNoTracking()
            .Where(p => p.OrganizationId == org.Id && p.Key == projectKey)
            .Select(p => new { p.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (project is null)
            return Response<EvaluateFlagResponseDto>.Fail("Project bulunamadı.");

        // 3) Environment (project scoped)
        var env = await context.ProjectEnvironments
            .AsNoTracking()
            .Where(e => e.ProjectId == project.Id && e.Key == environmentKey)
            .Select(e => new { e.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (env is null)
            return Response<EvaluateFlagResponseDto>.Fail("Environment bulunamadı.");

        // 4) Feature flag
        var flag = await context.FeatureFlags
            .AsNoTracking()
            .Where(f => f.ProjectId == project.Id && f.Key == flagKey)
            .Select(f => new { f.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (flag is null)
            return Response<EvaluateFlagResponseDto>.Fail("Feature flag bulunamadı.");

        // 5) Flag environment
        var flagEnv = await context.FeatureFlagEnvironments
            .AsNoTracking()
            .Where(fe => fe.FeatureFlagId == flag.Id && fe.ProjectEnvironmentId == env.Id)
            .Select(fe => new { fe.Id, fe.IsEnabled })
            .FirstOrDefaultAsync(cancellationToken);

        if (flagEnv is null)
            return Response<EvaluateFlagResponseDto>.Fail("Feature flag environment bulunamadı.");

        // 6) Segment targetings (MVP: flat AND, IsEnabled only)
        var targetings = await context.FeatureFlagSegmentTargetings
            .AsNoTracking()
            .Where(t => t.FeatureFlagEnvironmentId == flagEnv.Id && t.IsEnabled)
            .OrderByDescending(t => t.Priority)
            .Select(t => new { t.SegmentGroupId })
            .ToListAsync(cancellationToken);

        if (targetings.Any() && request.Traits is not null)
        {
            foreach (var t in targetings)
            {
                var rules = await context.SegmentRules
                    .AsNoTracking()
                    .Where(r => r.SegmentGroupId == t.SegmentGroupId)
                    .OrderBy(r => r.SortOrder)
                    .Select(r => new
                    {
                        r.TraitKey,
                        r.Operator,
                        r.Value,
                        r.ValueType
                    })
                    .ToListAsync(cancellationToken);

                if (rules.Count == 0)
                    continue;

                var match = rules.All(r =>
                    request.Traits.TryGetValue(r.TraitKey!, out var traitValue) &&
                    EvaluateRule(traitValue, r.Operator, r.Value, r.ValueType!.Value)
                );

                if (match)
                    return Response<EvaluateFlagResponseDto>.Ok(new EvaluateFlagResponseDto(true));
                
                return Response<EvaluateFlagResponseDto>.Ok(new EvaluateFlagResponseDto(false));
            }
        }

        // 7) No segment matched -> fallback to flag environment toggle
        return Response<EvaluateFlagResponseDto>.Ok(new EvaluateFlagResponseDto(flagEnv.IsEnabled));
    }
    
    private static bool EvaluateRule(
        string traitValue,
        string? op,
        string? ruleValue,
        SegmentValueType valueType)
    {
        ruleValue ??= string.Empty;

        // Operator is stored as string in DB (e.g., "Equals", "Contains").
        if (string.IsNullOrWhiteSpace(op) || !Enum.TryParse<SegmentOperator>(op, ignoreCase: true, out var parsedOp))
            return false;

        return parsedOp switch
        {
            SegmentOperator.Equals => string.Equals(traitValue, ruleValue, StringComparison.OrdinalIgnoreCase),
            SegmentOperator.NotEquals => !string.Equals(traitValue, ruleValue, StringComparison.OrdinalIgnoreCase),
            SegmentOperator.Contains => traitValue.Contains(ruleValue, StringComparison.OrdinalIgnoreCase),
            SegmentOperator.StartsWith => traitValue.StartsWith(ruleValue, StringComparison.OrdinalIgnoreCase),
            SegmentOperator.EndsWith => traitValue.EndsWith(ruleValue, StringComparison.OrdinalIgnoreCase),
            SegmentOperator.In => ruleValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(v => string.Equals(v, traitValue, StringComparison.OrdinalIgnoreCase)),
            _ => false
        };
    }
}