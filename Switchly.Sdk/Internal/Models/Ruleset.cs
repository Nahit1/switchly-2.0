namespace Switchly.Sdk;

public sealed record Ruleset(
    EnvironmentInfo Environment,
    IReadOnlyList<FlagDefinition> Flags
);

public sealed record EnvironmentInfo(Guid Id, string Key);

public sealed record FlagDefinition(
    Guid Id,
    string Key,
    bool EnvEnabled,
    RolloutKind DefaultRolloutKind,
    int DefaultRolloutPercentage,
    IReadOnlyList<Targeting> Targetings
);

public sealed record Targeting(
    int Priority,
    bool IsEnabled,
    RolloutKind RolloutKind,
    int RolloutPercentage,
    LogicalOperator LogicalOperator,
    IReadOnlyList<SegmentRule> Rules
);

public sealed record SegmentRule(
    string TraitKey,
    string Operator,
    string? Value,
    SegmentValueType? ValueType
);
