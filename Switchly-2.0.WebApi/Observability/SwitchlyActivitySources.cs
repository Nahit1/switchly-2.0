using System.Diagnostics;

namespace Switchly_2._0.WebApi.Observability;

// Custom OpenTelemetry ActivitySource'u. Switchly'ye özgü iş operasyonlarının
// (tracking ingest, analytics join, retention cleanup) ayrı span'lerle akması için.
// Program.cs'te `.AddSource(SwitchlyActivitySources.Name)` ile OTEL'e bağlanıyor.
public static class SwitchlyActivitySources
{
    public const string Name = "Switchly.Tracking";
    public static readonly ActivitySource Tracking = new(Name);
}
