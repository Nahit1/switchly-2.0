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

        // Exposure tracking pipeline. ExposureRecorder singleton — queue ve dedup
        // cache process ömrü boyunca yaşar. Sender transient — RulesetFetcher gibi
        // HttpClient factory'den her flush'ta yeni alır.
        services.AddSingleton<ExposureRecorder>(sp => new ExposureRecorder(
            sp.GetRequiredService<IOptions<SwitchlyOptions>>(),
            sp.GetRequiredService<ILogger<ExposureRecorder>>()
        ));

        services.AddTransient<ExposureSender>(sp => new ExposureSender(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName),
            sp.GetRequiredService<IOptions<SwitchlyOptions>>()
        ));

        // Conversion tracking pipeline — exposure'un kardeşi, ayrı queue + ayrı flusher.
        services.AddSingleton<ConversionRecorder>(sp => new ConversionRecorder(
            sp.GetRequiredService<IOptions<SwitchlyOptions>>(),
            sp.GetRequiredService<ILogger<ConversionRecorder>>()
        ));

        services.AddTransient<ConversionSender>(sp => new ConversionSender(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName),
            sp.GetRequiredService<IOptions<SwitchlyOptions>>()
        ));

        services.AddSingleton<SwitchlyClient>(sp => new SwitchlyClient(
            sp.GetRequiredService<RulesetCache>(),
            sp.GetRequiredService<FlagEvaluator>(),
            sp.GetRequiredService<ExposureRecorder>(),
            sp.GetRequiredService<ConversionRecorder>(),
            sp.GetRequiredService<IServiceScopeFactory>()
        ));

        services.AddHostedService<BackgroundRefresher>(sp => new BackgroundRefresher(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<RulesetCache>(),
            sp.GetRequiredService<IOptions<SwitchlyOptions>>(),
            sp.GetRequiredService<ILogger<BackgroundRefresher>>()
        ));

        services.AddHostedService<BackgroundFlusher>(sp => new BackgroundFlusher(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<ExposureRecorder>(),
            sp.GetRequiredService<IOptions<SwitchlyOptions>>(),
            sp.GetRequiredService<ILogger<BackgroundFlusher>>()
        ));

        services.AddHostedService<ConversionFlusher>(sp => new ConversionFlusher(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<ConversionRecorder>(),
            sp.GetRequiredService<IOptions<SwitchlyOptions>>(),
            sp.GetRequiredService<ILogger<ConversionFlusher>>()
        ));

        return services;
    }
}
