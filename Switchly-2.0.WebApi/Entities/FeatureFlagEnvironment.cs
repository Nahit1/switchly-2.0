using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Entities;

public class FeatureFlagEnvironment
{
    public Guid Id { get; set; }

    public Guid FeatureFlagId { get; set; }
    public FeatureFlag FeatureFlag { get; set; } = default!;

    public Guid EnvironmentId { get; set; }
    public Environment Environment { get; set; } = default!;

    public bool IsEnabled { get; set; }
    public RolloutKind DefaultRolloutKind { get; set; }
    public int DefaultRolloutPercentage { get; set; }  // 0–100

    public DateTimeOffset UpdatedAt { get; set; }
    
    public ICollection<FeatureFlagSegmentTargeting> SegmentTargetings { get; set; } = new List<FeatureFlagSegmentTargeting>();
}