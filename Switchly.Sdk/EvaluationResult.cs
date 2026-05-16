namespace Switchly.Sdk;

/// <summary>
/// Flag evaluation çıktısı. Boolean flag'lerde sadece IsOn anlamlı, VariantKey/PayloadJson null.
/// Multivariant flag'lerde IsOn=true ise hangi variant seçildiği VariantKey'de.
/// </summary>
public readonly record struct EvaluationResult(bool IsOn, string? VariantKey, string? PayloadJson)
{
    public static readonly EvaluationResult Off = new(false, null, null);
}
