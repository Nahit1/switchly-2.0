namespace Switchly_2._0.WebApi.Entities;

public class Variant
{
    public Guid Id { get; set; }
    public Guid FeatureFlagId { get; set; }
    public FeatureFlag FeatureFlag { get; set; } = default!;

    public string Key { get; set; } = default!;   // control, variant_a, a_config...
    public string? Name { get; set; }
    public string? PayloadJson { get; set; }      // JSONB karşılığı string, EF tarafında jsonb mapleriz
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<FeatureFlagEnvironmentVariantWeight> EnvironmentWeights { get; set; } = new List<FeatureFlagEnvironmentVariantWeight>();
    public ICollection<FeatureFlagSegmentTargetingVariantWeight> TargetingWeights { get; set; } = new List<FeatureFlagSegmentTargetingVariantWeight>();
}