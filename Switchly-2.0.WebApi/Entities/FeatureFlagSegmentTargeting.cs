using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Entities;

public class FeatureFlagSegmentTargeting
{
    public Guid Id { get; set; }

    public Guid FeatureFlagEnvironmentId { get; set; }
    public FeatureFlagEnvironment FeatureFlagEnvironment { get; set; } = default!;

    public Guid SegmentGroupId { get; set; }
    public SegmentGroup SegmentGroup { get; set; } = default!;

    public RolloutKind RolloutKind { get; set; }    // AllUsers, Percentage, Off
    public int RolloutPercentage { get; set; }      // 0–100
    public int Priority { get; set; }               // segment çakışmalarında öncelik
    public bool IsEnabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Multivariant flag'ler için bu targeting eşleştiğinde uygulanacak variant dağılımı. Boolean flag'lerde boş kalır.
    public ICollection<FeatureFlagSegmentTargetingVariantWeight> VariantWeights { get; set; } = new List<FeatureFlagSegmentTargetingVariantWeight>();
}