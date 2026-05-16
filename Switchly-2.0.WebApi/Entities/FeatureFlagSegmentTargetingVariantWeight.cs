namespace Switchly_2._0.WebApi.Entities;

public class FeatureFlagSegmentTargetingVariantWeight
{
    public Guid Id { get; set; }

    public Guid FeatureFlagSegmentTargetingId { get; set; }
    public FeatureFlagSegmentTargeting FeatureFlagSegmentTargeting { get; set; } = default!;

    public Guid VariantId { get; set; }
    public Variant Variant { get; set; } = default!;

    // 0–100. Bir targeting içindeki tüm variantların weight toplamı 100 olmalı (app-side validate).
    public int Weight { get; set; }
}
