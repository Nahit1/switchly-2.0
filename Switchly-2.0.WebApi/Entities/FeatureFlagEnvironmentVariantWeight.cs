namespace Switchly_2._0.WebApi.Entities;

public class FeatureFlagEnvironmentVariantWeight
{
    public Guid Id { get; set; }

    public Guid FeatureFlagEnvironmentId { get; set; }
    public FeatureFlagEnvironment FeatureFlagEnvironment { get; set; } = default!;

    public Guid VariantId { get; set; }
    public Variant Variant { get; set; } = default!;

    // 0–100. Bir env içindeki tüm variantların weight toplamı 100 olmalı (app-side validate).
    public int Weight { get; set; }
}
