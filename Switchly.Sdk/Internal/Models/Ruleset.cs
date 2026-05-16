namespace Switchly.Sdk;

public sealed record Ruleset(
    EnvironmentInfo Environment,
    IReadOnlyList<FlagDefinition> Flags
);

public sealed record EnvironmentInfo(Guid Id, string Key);

public sealed record FlagDefinition(
    Guid Id,
    string Key,
    FeatureFlagType Type,
    bool EnvEnabled,
    RolloutKind DefaultRolloutKind,
    int DefaultRolloutPercentage,
    IReadOnlyList<VariantInfo> Variants,
    IReadOnlyList<VariantWeight> EnvVariantWeights,
    IReadOnlyList<Targeting> Targetings
);

public sealed record VariantInfo(
    Guid Id,
    string Key,
    string? Name,
    string? PayloadJson,
    int SortOrder
);

public sealed record VariantWeight(Guid VariantId, int Weight);

public sealed record Targeting(
    int Priority,
    bool IsEnabled,
    RolloutKind RolloutKind,
    int RolloutPercentage,
    LogicalOperator LogicalOperator,
    IReadOnlyList<SegmentRule> Rules,
    IReadOnlyList<VariantWeight> VariantWeights
);

public sealed record SegmentRule(
    string TraitKey,
    string Operator,
    string? Value,
    SegmentValueType? ValueType
);
