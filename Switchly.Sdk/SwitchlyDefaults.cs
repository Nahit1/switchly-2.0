namespace Switchly.Sdk;

internal static class SwitchlyDefaults
{
    // Switchly production API. Geliştirici override etmek istemezse bu kullanılır.
    public const string BaseUrl = "http://46.62.219.92:8080";

    public static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);
}
