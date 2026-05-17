namespace Switchly_2._0.WebApi.Entities;

// Domain conversion event: kullanıcı bizim için anlamlı bir eylem yaptı (checkout,
// signup, subscribe, vs). FlagId YOK — conversion flag-agnostic; analytics sorgusunda
// FlagExposureEvents ile join'lenip variant attribution hesaplanır.
public class ConversionEvent
{
    public Guid Id { get; set; }

    // Exposure ile aynı denormalize hiyerarşi: analytics join-siz tarama yapsın.
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public Guid ProjectEnvironmentId { get; set; }
    public ProjectEnvironment ProjectEnvironment { get; set; } = default!;

    public string UserKey { get; set; } = default!;

    // Domain event ismi: "checkout_completed", "signup", "subscribe", "button_clicked"...
    // Whitelist yok; consumer ne yazarsa onu kabul ediyoruz (MVP).
    public string EventName { get; set; } = default!;

    // Revenue ya da numeric metric (price, count, score). null = sayım metric'i.
    public decimal? Value { get; set; }

    // Opsiyonel metadata. İleride filter/drill-down için ("only mobile", "amount > 100").
    public string? PropertiesJson { get; set; }

    // SDK eval/track anı (client-reported); server ingestion anı değil.
    public DateTimeOffset OccurredAt { get; set; }
}
