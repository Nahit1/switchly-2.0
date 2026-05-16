using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Entities;

public class FeatureFlagEnvironment
{
    public Guid Id { get; set; }

    public Guid FeatureFlagId { get; set; }
    public FeatureFlag FeatureFlag { get; set; } = default!;

    public Guid ProjectEnvironmentId { get; set; }
    public ProjectEnvironment ProjectEnvironment { get; set; } = default!;

    public bool IsEnabled { get; set; }
    public RolloutKind DefaultRolloutKind { get; set; }
    public int DefaultRolloutPercentage { get; set; }  // 0–100

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<FeatureFlagSegmentTargeting> SegmentTargetings { get; set; } = new List<FeatureFlagSegmentTargeting>();

    // Multivariant flag'ler için env-level default variant dağılımı. Boolean flag'lerde boş kalır.
    public ICollection<FeatureFlagEnvironmentVariantWeight> VariantWeights { get; set; } = new List<FeatureFlagEnvironmentVariantWeight>();
}