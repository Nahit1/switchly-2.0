namespace Switchly.Sdk;

public sealed class SwitchlyOptions
{
    /// <summary>Organization's public key (e.g. "pub_ABC123").</summary>
    public string PublicKey { get; set; } = "";

    /// <summary>Project's slug-style key (NOT the GUID Id).</summary>
    public string ProjectKey { get; set; } = "";

    /// <summary>Environment slug (e.g. "dev", "prod").</summary>
    public string EnvironmentKey { get; set; } = "";

    /// <summary>
    /// Override the Switchly API base URL. Production'da bunu set etmen gerekmez —
    /// default Switchly servisini hedefler. Self-hosted veya yerel backend için override et.
    /// </summary>
    public string BaseUrl { get; set; } = SwitchlyDefaults.BaseUrl;

    /// <summary>How often the SDK refreshes the ruleset in the background. Default: 30s.</summary>
    public TimeSpan PollingInterval { get; set; } = SwitchlyDefaults.PollingInterval;
}
