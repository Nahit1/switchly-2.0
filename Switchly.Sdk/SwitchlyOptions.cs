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

    /// <summary>
    /// Exposure event tracking aç/kapa. true ise IsOn/GetVariant çağrıları bir
    /// (user, flag, variant) maruziyetini batch'leyip backend'e gönderir.
    /// </summary>
    public bool TrackingEnabled { get; set; } = true;

    /// <summary>Background flusher event queue'yu hangi sıklıkta boşaltıp POST etsin. Default: 10sn.</summary>
    public TimeSpan FlushInterval { get; set; } = SwitchlyDefaults.FlushInterval;

    /// <summary>Tek POST'ta gönderilecek maksimum event sayısı. Backend MaxBatchSize'ıyla hizalı olmalı.</summary>
    public int FlushBatchSize { get; set; } = SwitchlyDefaults.FlushBatchSize;

    /// <summary>
    /// Queue'da tutulabilecek maksimum event. Aşıldığında yeni event'ler drop edilir
    /// (fail-soft; app slow-down etmesin diye consumer'ı bloklamıyoruz).
    /// </summary>
    public int MaxQueueSize { get; set; } = SwitchlyDefaults.MaxQueueSize;

    /// <summary>
    /// (userKey, flagKey, outcome) kombinasyonunun yeniden event üretebilmesi için
    /// beklenecek süre. Default: 5dk. Düşürürsen daha sık event, network artar.
    /// </summary>
    public TimeSpan DedupTtl { get; set; } = SwitchlyDefaults.DedupTtl;
}
