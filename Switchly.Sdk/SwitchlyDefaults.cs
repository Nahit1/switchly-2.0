namespace Switchly.Sdk;

internal static class SwitchlyDefaults
{
    // Switchly production API. Geliştirici override etmek istemezse bu kullanılır.
    public const string BaseUrl = "http://46.62.219.92:8080";

    public static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);

    public static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(10);
    public const int FlushBatchSize = 500;
    public const int MaxQueueSize = 10_000;
    public static readonly TimeSpan DedupTtl = TimeSpan.FromMinutes(5);
}
