namespace Switchly.Sdk.Internal;

// SDK içinde producer (recorder) → queue → consumer (flusher) arasında dolaşan event şekli.
// İsteyerek internal: consumer'ın bu tipi görmesine veya ona göre kod yazmasına gerek yok.
internal sealed record ExposureEvent(
    string UserKey,
    string FlagKey,
    string? VariantKey,
    bool IsOn,
    DateTimeOffset OccurredAt
);
