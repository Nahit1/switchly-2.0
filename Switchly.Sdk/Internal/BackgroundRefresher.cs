using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Switchly.Sdk.Internal;

internal sealed class BackgroundRefresher(
    IServiceScopeFactory scopeFactory,
    RulesetCache cache,
    IOptions<SwitchlyOptions> options,
    ILogger<BackgroundRefresher> log
) : BackgroundService
{
    private readonly SwitchlyOptions _opts = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial fetch — uygulama açılır açılmaz.
        await TryFetchOnceAsync(stoppingToken);

        // Sonra periyodik refresh.
        using var timer = new PeriodicTimer(_opts.PollingInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await TryFetchOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutdown — bekleniyor, sessizce çık.
        }
    }

    private async Task TryFetchOnceAsync(CancellationToken ct)
    {
        try
        {
            // RulesetFetcher transient (HttpClient ile birlikte) → scope üzerinden alınır.
            using var scope = scopeFactory.CreateScope();
            var fetcher = scope.ServiceProvider.GetRequiredService<RulesetFetcher>();
            var ruleset = await fetcher.FetchAsync(ct);
            cache.Set(ruleset);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Fail-soft: eski cache aynen kalır, app çalışmaya devam eder.
            log.LogWarning(ex, "Switchly ruleset fetch failed; previous cache retained.");
        }
    }
}
