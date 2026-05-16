using System.Security.Cryptography;
using System.Text;
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
    string? UserKey,
    Dictionary<string, string>? Traits
) : IRequest<Response<EvaluateFlagResponseDto>>;

public record EvaluateFlagResponseDto(
    bool IsEnabled,
    string? VariantKey,
    string? VariantPayloadJson
);

public class EvaluateFlagHandler(SwitchlyDbContext context)
    : IRequestHandler<EvaluateFlagRequest, Response<EvaluateFlagResponseDto>>
{
    public async Task<Response<EvaluateFlagResponseDto>> Handle(EvaluateFlagRequest request, CancellationToken ct)
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

        // Tek query: org→project→env→flag→flagEnv chain + eval için gereken tüm alt-veri.
        var snapshot = await context.FeatureFlagEnvironments
            .AsNoTracking()
            .Where(fe => fe.FeatureFlag.Key == flagKey
                         && fe.ProjectEnvironment.Key == environmentKey
                         && fe.FeatureFlag.Project.Key == projectKey
                         && fe.FeatureFlag.Project.Organization.PublicKey == publicKey
                         && !fe.FeatureFlag.IsArchived)
            .Select(fe => new FlagEvalSnapshot(
                fe.FeatureFlag.Key,
                fe.FeatureFlag.Type,
                fe.IsEnabled,
                fe.DefaultRolloutKind,
                fe.DefaultRolloutPercentage,
                fe.FeatureFlag.Variants
                    .OrderBy(v => v.SortOrder)
                    .Select(v => new VariantSnapshot(v.Id, v.Key, v.PayloadJson))
                    .ToList(),
                fe.VariantWeights
                    .Select(w => new VariantWeightSnapshot(w.VariantId, w.Weight))
                    .ToList(),
                fe.SegmentTargetings
                    .Where(t => t.IsEnabled)
                    .OrderByDescending(t => t.Priority)
                    .Select(t => new TargetingSnapshot(
                        t.RolloutKind,
                        t.RolloutPercentage,
                        t.SegmentGroup.LogicalOperator,
                        t.SegmentGroup.Rules
                            .Where(r => r.NodeType == SegmentNodeType.Condition
                                        && r.TraitKey != null
                                        && r.Operator != null)
                            .OrderBy(r => r.SortOrder)
                            .Select(r => new RuleSnapshot(r.TraitKey!, r.Operator!, r.Value))
                            .ToList(),
                        t.VariantWeights
                            .Select(w => new VariantWeightSnapshot(w.VariantId, w.Weight))
                            .ToList()
                    ))
                    .ToList()
            ))
            .FirstOrDefaultAsync(ct);

        if (snapshot is null)
            return Response<EvaluateFlagResponseDto>.Fail("Flag bulunamadı.");

        var result = Evaluate(snapshot, request.UserKey, request.Traits);
        return Response<EvaluateFlagResponseDto>.Ok(result);
    }

    private static EvaluateFlagResponseDto Evaluate(
        FlagEvalSnapshot flag,
        string? userKey,
        IReadOnlyDictionary<string, string>? traits)
    {
        if (!flag.EnvEnabled)
            return Off;

        traits ??= EmptyTraits;

        foreach (var t in flag.Targetings)
        {
            if (!RulesMatch(t.Rules, traits, t.LogicalOperator))
                continue;

            return flag.Type == FeatureFlagType.Multivariant
                ? PickVariant(flag, t.VariantWeights, userKey)
                : Boolean(ApplyRollout(t.RolloutKind, t.RolloutPercentage, flag.Key, userKey));
        }

        return flag.Type == FeatureFlagType.Multivariant
            ? PickVariant(flag, flag.EnvVariantWeights, userKey)
            : Boolean(ApplyRollout(flag.DefaultRolloutKind, flag.DefaultRolloutPercentage, flag.Key, userKey));
    }

    private static readonly EvaluateFlagResponseDto Off = new(false, null, null);
    private static readonly Dictionary<string, string> EmptyTraits = new(0);

    private static EvaluateFlagResponseDto Boolean(bool isOn) =>
        isOn ? new EvaluateFlagResponseDto(true, null, null) : Off;

    private static EvaluateFlagResponseDto PickVariant(
        FlagEvalSnapshot flag,
        List<VariantWeightSnapshot> weights,
        string? userKey)
    {
        if (weights.Count == 0) return Off;
        if (string.IsNullOrWhiteSpace(userKey)) return Off;

        var bucket = Bucket(flag.Key, userKey);
        var weightByVariantId = weights.ToDictionary(w => w.VariantId, w => w.Weight);

        var cumulative = 0;
        foreach (var v in flag.Variants)
        {
            if (!weightByVariantId.TryGetValue(v.Id, out var w) || w <= 0) continue;
            cumulative += w;
            if (bucket < cumulative)
                return new EvaluateFlagResponseDto(true, v.Key, v.PayloadJson);
        }

        return Off;
    }

    private static bool ApplyRollout(RolloutKind kind, int percentage, string flagKey, string? userKey)
        => kind switch
        {
            RolloutKind.AllUsers => true,
            RolloutKind.Off => false,
            RolloutKind.Percentage => InBucket(flagKey, userKey, percentage),
            _ => false
        };

    private static bool InBucket(string flagKey, string? userKey, int percentage)
    {
        if (percentage <= 0) return false;
        if (percentage >= 100) return true;
        if (string.IsNullOrWhiteSpace(userKey)) return false;
        return Bucket(flagKey, userKey) < percentage;
    }

    private static int Bucket(string flagKey, string userKey)
    {
        var input = Encoding.UTF8.GetBytes($"{flagKey}:{userKey}");
        var hash = SHA256.HashData(input);
        return (int)(BitConverter.ToUInt32(hash, 0) % 100);
    }

    private static bool RulesMatch(
        List<RuleSnapshot> rules,
        IReadOnlyDictionary<string, string> traits,
        LogicalOperator op)
    {
        if (rules.Count == 0) return false;

        return op == LogicalOperator.Or
            ? rules.Any(r => SingleRuleMatches(r, traits))
            : rules.All(r => SingleRuleMatches(r, traits));
    }

    private static bool SingleRuleMatches(RuleSnapshot r, IReadOnlyDictionary<string, string> traits)
    {
        if (!traits.TryGetValue(r.TraitKey, out var traitValue)) return false;
        return CompareTrait(traitValue, r.Operator, r.Value ?? string.Empty);
    }

    private static bool CompareTrait(string traitValue, string op, string ruleValue)
    {
        if (!Enum.TryParse<SegmentOperator>(op, ignoreCase: true, out var parsedOp))
            return false;

        return parsedOp switch
        {
            SegmentOperator.Equals => string.Equals(traitValue, ruleValue, StringComparison.OrdinalIgnoreCase),
            SegmentOperator.NotEquals => !string.Equals(traitValue, ruleValue, StringComparison.OrdinalIgnoreCase),
            SegmentOperator.Contains => traitValue.Contains(ruleValue, StringComparison.OrdinalIgnoreCase),
            SegmentOperator.StartsWith => traitValue.StartsWith(ruleValue, StringComparison.OrdinalIgnoreCase),
            SegmentOperator.EndsWith => traitValue.EndsWith(ruleValue, StringComparison.OrdinalIgnoreCase),
            SegmentOperator.In => ruleValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(v => string.Equals(v, traitValue, StringComparison.OrdinalIgnoreCase)),
            _ => false
        };
    }

    private sealed record FlagEvalSnapshot(
        string Key,
        FeatureFlagType Type,
        bool EnvEnabled,
        RolloutKind DefaultRolloutKind,
        int DefaultRolloutPercentage,
        List<VariantSnapshot> Variants,
        List<VariantWeightSnapshot> EnvVariantWeights,
        List<TargetingSnapshot> Targetings
    );

    private sealed record VariantSnapshot(Guid Id, string Key, string? PayloadJson);

    private sealed record VariantWeightSnapshot(Guid VariantId, int Weight);

    private sealed record TargetingSnapshot(
        RolloutKind RolloutKind,
        int RolloutPercentage,
        LogicalOperator LogicalOperator,
        List<RuleSnapshot> Rules,
        List<VariantWeightSnapshot> VariantWeights
    );

    private sealed record RuleSnapshot(string TraitKey, string Operator, string? Value);
}
