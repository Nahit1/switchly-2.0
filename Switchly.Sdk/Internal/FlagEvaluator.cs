using System.Security.Cryptography;
using System.Text;

namespace Switchly.Sdk.Internal;

internal sealed class FlagEvaluator
{
    public EvaluationResult Evaluate(
        FlagDefinition flag,
        string? userKey,
        IReadOnlyDictionary<string, string>? traits)
    {
        // Master kill-switch: env'de pause edilmişse hiç eval etme.
        if (!flag.EnvEnabled)
            return EvaluationResult.Off;

        traits ??= EmptyTraits;

        // Targeting'ler priority desc'te dener; ruleset endpoint zaten sıralı dönüyor,
        // yine de defansif sıralama.
        foreach (var t in flag.Targetings
                              .Where(x => x.IsEnabled)
                              .OrderByDescending(x => x.Priority))
        {
            if (!RulesMatch(t.Rules, traits, t.LogicalOperator))
                continue;

            return flag.Type == FeatureFlagType.Multivariant
                ? PickVariant(flag, t.VariantWeights, userKey)
                : Boolean(ApplyRollout(t.RolloutKind, t.RolloutPercentage, flag.Key, userKey));
        }

        // Hiçbir targeting eşleşmedi → env-level default.
        return flag.Type == FeatureFlagType.Multivariant
            ? PickVariant(flag, flag.EnvVariantWeights, userKey)
            : Boolean(ApplyRollout(flag.DefaultRolloutKind, flag.DefaultRolloutPercentage, flag.Key, userKey));
    }

    private static readonly Dictionary<string, string> EmptyTraits = new(0);

    private static bool RulesMatch(
        IReadOnlyList<SegmentRule> rules,
        IReadOnlyDictionary<string, string> traits,
        LogicalOperator op)
    {
        if (rules.Count == 0) return false;

        return op == LogicalOperator.Or
            ? rules.Any(r => SingleRuleMatches(r, traits))
            : rules.All(r => SingleRuleMatches(r, traits));
        // Not bilerek MVP'de işlenmiyor — segment'ten Group/Not akışı yok.
    }

    private static bool SingleRuleMatches(
        SegmentRule r,
        IReadOnlyDictionary<string, string> traits)
    {
        if (!traits.TryGetValue(r.TraitKey, out var traitValue))
            return false;
        return CompareTrait(traitValue, r.Operator, r.Value ?? string.Empty);
    }

    private static bool ApplyRollout(
        RolloutKind kind,
        int percentage,
        string flagKey,
        string? userKey)
        => kind switch
        {
            RolloutKind.AllUsers   => true,
            RolloutKind.Off        => false,
            RolloutKind.Percentage => InBucket(flagKey, userKey, percentage),
            _                      => false
        };

    private static EvaluationResult Boolean(bool isOn) =>
        isOn ? new EvaluationResult(true, null, null) : EvaluationResult.Off;

    /// <summary>
    /// Multivariant bucket: hash(flagKey:userKey) mod 100 ile bir bucket bulup,
    /// variants'ı SortOrder'a göre cumulative weight üzerinden eşler.
    /// userKey yoksa stable bucket hesaplanamaz → safe-default Off.
    /// </summary>
    private static EvaluationResult PickVariant(
        FlagDefinition flag,
        IReadOnlyList<VariantWeight> weights,
        string? userKey)
    {
        if (weights.Count == 0) return EvaluationResult.Off;
        if (string.IsNullOrWhiteSpace(userKey)) return EvaluationResult.Off;

        var bucket = Bucket(flag.Key, userKey);

        // Variants'ı SortOrder ile sırala — server ve client aynı order'da bucket'lasın diye şart.
        var orderedVariants = flag.Variants.OrderBy(v => v.SortOrder).ToList();
        var weightByVariantId = weights.ToDictionary(w => w.VariantId, w => w.Weight);

        var cumulative = 0;
        foreach (var v in orderedVariants)
        {
            if (!weightByVariantId.TryGetValue(v.Id, out var w) || w <= 0) continue;
            cumulative += w;
            if (bucket < cumulative)
                return new EvaluationResult(true, v.Key, v.PayloadJson);
        }

        return EvaluationResult.Off;
    }

    private static bool InBucket(string flagKey, string? userKey, int percentage)
    {
        if (percentage <= 0)   return false;
        if (percentage >= 100) return true;

        // Stable bucket için userKey şart. Yoksa percentage hesaplanamaz → safe default false.
        if (string.IsNullOrWhiteSpace(userKey))
            return false;

        return Bucket(flagKey, userKey) < percentage;
    }

    private static int Bucket(string flagKey, string userKey)
    {
        var input = Encoding.UTF8.GetBytes($"{flagKey}:{userKey}");
        var hash = SHA256.HashData(input);
        return (int)(BitConverter.ToUInt32(hash, 0) % 100);
    }

    private static bool CompareTrait(string traitValue, string op, string ruleValue)
        => op switch
        {
            "Equals"     => string.Equals(traitValue, ruleValue, StringComparison.OrdinalIgnoreCase),
            "NotEquals"  => !string.Equals(traitValue, ruleValue, StringComparison.OrdinalIgnoreCase),
            "Contains"   => traitValue.Contains(ruleValue, StringComparison.OrdinalIgnoreCase),
            "StartsWith" => traitValue.StartsWith(ruleValue, StringComparison.OrdinalIgnoreCase),
            "EndsWith"   => traitValue.EndsWith(ruleValue, StringComparison.OrdinalIgnoreCase),
            "In"         => ruleValue
                                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Any(v => string.Equals(v, traitValue, StringComparison.OrdinalIgnoreCase)),
            _            => false
        };
}
