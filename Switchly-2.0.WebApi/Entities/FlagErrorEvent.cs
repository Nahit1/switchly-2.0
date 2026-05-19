using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Entities;

// Bir flag'le ilişkilendirilmiş external error event'i (Sentry/Datadog/custom monitoring'den).
// Progressive rollout guardrail service bu tabloyu okuyup eşik aşılırsa schedule'ı rollback eder.
public class FlagErrorEvent
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public Guid ProjectEnvironmentId { get; set; }
    public ProjectEnvironment ProjectEnvironment { get; set; } = default!;

    public Guid FeatureFlagId { get; set; }
    public FeatureFlag FeatureFlag { get; set; } = default!;

    public ErrorSeverity Severity { get; set; }
    public string Message { get; set; } = default!;
    public string? Source { get; set; }              // "sentry", "datadog", "custom"
    public string? PropertiesJson { get; set; }       // opsiyonel metadata
    public DateTimeOffset OccurredAt { get; set; }
}
