using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Switchly.Sdk.Internal;

// BackgroundFlusher'ın simetriği — bu kez ConversionRecorder'ın queue'sundan drain edip
// /api/track/conversions'a POST'luyor. İki ayrı flusher tutmamızın sebebi: pipeline
// bağımsızlığı. Birinin retry/backpressure davranışı diğerini etkilemesin diye.
internal sealed class ConversionFlusher(
    IServiceScopeFactory scopeFactory,
    ConversionRecorder recorder,
    IOptions<SwitchlyOptions> options,
    ILogger<ConversionFlusher> log
) : BackgroundService
{
    private readonly SwitchlyOptions _opts = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_opts.TrackingEnabled) return;

        using var timer = new PeriodicTimer(_opts.FlushInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await FlushOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutdown — normal akış.
        }

        // Best-effort final drain (BackgroundFlusher ile aynı strateji).
        try
        {
            using var drainCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await FlushOnceAsync(drainCts.Token);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Final Switchly conversion flush during shutdown was incomplete.");
        }
    }

    private async Task FlushOnceAsync(CancellationToken ct)
    {
        var batch = DrainBatch();
        if (batch.Count == 0) return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ConversionSender>();
            await sender.SendAsync(batch, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Switchly conversion batch send failed; {Count} events lost.", batch.Count);
        }
    }

    private List<ConversionEvent> DrainBatch()
    {
        var batch = new List<ConversionEvent>(_opts.FlushBatchSize);
        while (batch.Count < _opts.FlushBatchSize && recorder.Reader.TryRead(out var ev))
            batch.Add(ev);
        return batch;
    }
}
