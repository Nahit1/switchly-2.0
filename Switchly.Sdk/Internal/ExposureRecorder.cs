using System.Threading.Channels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Switchly.Sdk.Internal;

/// <summary>
/// Tracking pipeline'ının ön ucu: SwitchlyClient her evaluate'den sonra
/// TryRecord(...) çağırır. Recorder de-dup filtresinden geçirip bounded queue'ya yazar;
/// BackgroundFlusher queue'yu drain edip backend'e POST'lar.
///
/// Hot path: TryRecord asla bloklamamalı, asla exception fırlatmamalı.
/// Tracking failure consumer'ın uygulamasını ASLA bozmamalı.
/// </summary>
internal sealed class ExposureRecorder : IDisposable
{
    private readonly SwitchlyOptions _opts;
    private readonly ILogger<ExposureRecorder> _log;
    private readonly IMemoryCache _dedup;
    private readonly Channel<ExposureEvent> _channel;
    private long _droppedCount;

    public ExposureRecorder(
        IOptions<SwitchlyOptions> options,
        ILogger<ExposureRecorder> log)
    {
        _opts = options.Value;
        _log = log;

        // Size limit: TTL geç gelse veya patolojik trafikte bellek patlamasın.
        // 1M entry × ~100 byte = ~100MB worst case; gerçek hayatta çok daha düşük.
        _dedup = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1_000_000 });

        _channel = Channel.CreateBounded<ExposureEvent>(new BoundedChannelOptions(_opts.MaxQueueSize)
        {
            // Queue dolarsa yeni event'i drop et — IsOn çağrısı asla bloklanmasın.
            FullMode = BoundedChannelFullMode.DropWrite,
            // BackgroundFlusher tek reader, birden fazla request thread'i writer.
            SingleReader = true,
            SingleWriter = false,
            // Reader continuation'ı producer thread'inde çalışmasın (consumer'ın hot path'ini koruyalım).
            AllowSynchronousContinuations = false
        });
    }

    public ChannelReader<ExposureEvent> Reader => _channel.Reader;

    public void TryRecord(string? userKey, string flagKey, string? variantKey, bool isOn)
    {
        // Erken çıkışlar — tracking kapalı veya kayıt anlamlı değilse hiç maliyet ödeme.
        if (!_opts.TrackingEnabled) return;
        if (string.IsNullOrWhiteSpace(userKey)) return;       // userKey olmadan attribute edemiyoruz
        if (string.IsNullOrWhiteSpace(flagKey)) return;

        try
        {
            var dedupKey = BuildDedupKey(userKey, flagKey, variantKey, isOn);

            // De-dup: aynı (user, flag, outcome) son DedupTtl içinde gördüysek skip.
            if (_dedup.TryGetValue(dedupKey, out _))
                return;

            _dedup.Set(dedupKey, true, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _opts.DedupTtl,
                Size = 1
            });

            var ev = new ExposureEvent(
                UserKey: userKey,
                FlagKey: flagKey,
                VariantKey: variantKey,
                IsOn: isOn,
                OccurredAt: DateTimeOffset.UtcNow);

            if (!_channel.Writer.TryWrite(ev))
            {
                // Queue dolu. Drop counter'ı artır; spam log etmemek için her 1000'de bir uyar.
                var dropped = Interlocked.Increment(ref _droppedCount);
                if (dropped % 1000 == 1)
                    _log.LogWarning(
                        "Switchly exposure queue is full; dropping events (total dropped since startup: {Dropped}).",
                        dropped);
            }
        }
        catch (Exception ex)
        {
            // Hiçbir tracking hatası uygulamaya sızmasın. Sessizce logla.
            _log.LogWarning(ex, "Switchly exposure recording failed; event ignored.");
        }
    }

    private static string BuildDedupKey(string userKey, string flagKey, string? variantKey, bool isOn)
    {
        // Outcome multivariant'ta variantKey, boolean'da on/off.
        // Pipe ayırıcı: userKey/flagKey'lerde nadir, kollizyon riski düşük.
        var outcome = variantKey ?? (isOn ? "on" : "off");
        return string.Concat(userKey, "|", flagKey, "|", outcome);
    }

    public void Dispose()
    {
        // Yeni event kabul edilmesin; flusher kalan'ı drain etsin.
        _channel.Writer.TryComplete();
        _dedup.Dispose();
    }
}
