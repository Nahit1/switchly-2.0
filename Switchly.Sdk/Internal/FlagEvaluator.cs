using System.Security.Cryptography;
using System.Text;

namespace Switchly.Sdk.Internal;

internal sealed class FlagEvaluator
{
    public bool Evaluate(
        FlagDefinition flag,
        string? userKey,
        IReadOnlyDictionary<string, string>? traits)
    {
        // Master kill-switch: env'de pause edilmişse hiç eval etme.
        if (!flag.EnvEnabled)
            return false;

        traits ??= EmptyTraits;

        // Targeting'ler priority desc'te dener; ruleset endpoint zaten sıralı dönüyor,
        // yine de defansif sıralama.
        foreach (var t in flag.Targetings
                              .Where(x => x.IsEnabled)
                              .OrderByDescending(x => x.Priority))
        {
            if (RulesAllMatch(t.Rules, traits))
                return ApplyRollout(t.RolloutKind, t.RolloutPercentage, flag.Key, userKey);
        }

        // Hiçbir targeting eşleşmedi → env-level default.
        return ApplyRollout(flag.DefaultRolloutKind, flag.DefaultRolloutPercentage, flag.Key, userKey);
    }

    private static readonly Dictionary<string, string> EmptyTraits = new(0);

    private static bool RulesAllMatch(
        IReadOnlyList<SegmentRule> rules,
        IReadOnlyDictionary<string, string> traits)
    {
        if (rules.Count == 0) return false;

        foreach (var r in rules)
        {
            if (!traits.TryGetValue(r.TraitKey, out var traitValue))
                return false;
            if (!CompareTrait(traitValue, r.Operator, r.Value ?? string.Empty))
                return false;
        }
        return true;
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
