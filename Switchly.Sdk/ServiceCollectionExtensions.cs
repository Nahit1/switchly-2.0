using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Switchly.Sdk.Internal;

namespace Switchly.Sdk;

public static class ServiceCollectionExtensions
{
    private const string HttpClientName = "Switchly";

    public static IServiceCollection AddSwitchly(
        this IServiceCollection services,
        Action<SwitchlyOptions> configure)
    {
        services.Configure(configure);

        // Named HttpClient — typed AddHttpClient<RulesetFetcher>() yerine factory pattern
        // kullanıyoruz çünkü internal class'ı DI reflection'ı dış assembly'den göremez.
        services.AddHttpClient(HttpClientName);

        // Factory delegate'leri Switchly.Sdk içinde, internal ctor'lara erişebiliyor.
        services.AddTransient<RulesetFetcher>(sp => new RulesetFetcher(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName),
            sp.GetRequiredService<IOptions<SwitchlyOptions>>()
        ));

        services.AddSingleton<RulesetCache>(_ => new RulesetCache());
        services.AddSingleton<FlagEvaluator>(_ => new FlagEvaluator());

        services.AddSingleton<SwitchlyClient>(sp => new SwitchlyClient(
            sp.GetRequiredService<RulesetCache>(),
            sp.GetRequiredService<FlagEvaluator>(),
            sp.GetRequiredService<IServiceScopeFactory>()
        ));

        services.AddHostedService<BackgroundRefresher>(sp => new BackgroundRefresher(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<RulesetCache>(),
            sp.GetRequiredService<IOptions<SwitchlyOptions>>(),
            sp.GetRequiredService<ILogger<BackgroundRefresher>>()
        ));

        return services;
    }
}
