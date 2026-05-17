namespace Switchly.Sdk.Internal;

// SDK içinde producer (recorder) → queue → consumer (flusher) arasında dolaşan
// conversion event şekli. PropertiesJson burada ham JSON string olarak tutuluyor
// (Track API'sinde dictionary alıp serialize ediyoruz, queue'da string).
internal sealed record ConversionEvent(
    string UserKey,
    string EventName,
    decimal? Value,
    string? PropertiesJson,
    DateTimeOffset OccurredAt
);
