using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Switchly.Sdk.Internal;

/// <summary>
/// Conversion event'lerinin ön ucu. SwitchlyClient.Track(...) çağrısı recorder'a düşer,
/// recorder bounded queue'ya yazar; BackgroundConversionFlusher drain edip backend'e yollar.
///
/// ExposureRecorder'dan farkı: **de-dup YOK**. Exposure milyon kez tetiklenir; conversion
/// gerçek bir eylem (checkout, signup) — her biri ayrı row olmalı. Aynı user 1 dakikada
/// 3 kez checkout yapıyorsa 3 satır yazarız.
/// </summary>
internal sealed class ConversionRecorder
{
    private readonly SwitchlyOptions _opts;
    private readonly ILogger<ConversionRecorder> _log;
    private readonly Channel<ConversionEvent> _channel;
    private long _droppedCount;

    public ConversionRecorder(
        IOptions<SwitchlyOptions> options,
        ILogger<ConversionRecorder> log)
    {
        _opts = options.Value;
        _log = log;

        _channel = Channel.CreateBounded<ConversionEvent>(new BoundedChannelOptions(_opts.MaxQueueSize)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
    }

    public ChannelReader<ConversionEvent> Reader => _channel.Reader;

    public void TryRecord(
        string userKey,
        string eventName,
        decimal? value,
        string? propertiesJson)
    {
        if (!_opts.TrackingEnabled) return;
        if (string.IsNullOrWhiteSpace(userKey)) return;
        if (string.IsNullOrWhiteSpace(eventName)) return;

        try
        {
            var ev = new ConversionEvent(
                UserKey: userKey,
                EventName: eventName,
                Value: value,
                PropertiesJson: propertiesJson,
                OccurredAt: DateTimeOffset.UtcNow);

            if (!_channel.Writer.TryWrite(ev))
            {
                var dropped = Interlocked.Increment(ref _droppedCount);
                if (dropped % 1000 == 1)
                    _log.LogWarning(
                        "Switchly conversion queue is full; dropping events (total dropped: {Dropped}).",
                        dropped);
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Switchly conversion recording failed; event ignored.");
        }
    }
}
