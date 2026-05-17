using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Switchly.Sdk;

// Switchly SDK için test console'u.
// Hedef: distribution + stable bucketing + exposure tracking pipeline'ı end-to-end doğrulamak.
//
// Kullanım: dotnet run -- [userPrefix]   (default: "user")
// Farklı prefix = farklı user havuzu = DB'de farklı satırlar.

var userPrefix = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
    ? args[0]
    : "user";

Console.WriteLine($"User prefix: \"{userPrefix}\"\n");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSwitchly(opts =>
        {
            opts.BaseUrl = "http://localhost:8080";
            opts.PublicKey = "pub_9E9D4748D135CE96838E4D2508BBC785";
            opts.ProjectKey = "611d18c6-3fb9-4c83-a904-fac319da0776";
            opts.EnvironmentKey = "dev";

            // Test sırasında hızlı feedback için default'lardan agresif:
            opts.PollingInterval = TimeSpan.FromSeconds(3);
            opts.FlushInterval = TimeSpan.FromSeconds(2);
        });
    })
    .Build();

await host.StartAsync();
var client = host.Services.GetRequiredService<SwitchlyClient>();

// Initial ruleset fetch'i bekle — BackgroundRefresher startup'ta hemen çekiyor.
Console.WriteLine("Initial ruleset fetch bekleniyor...");
var startedAt = DateTimeOffset.UtcNow;
while (!client.IsReady && DateTimeOffset.UtcNow - startedAt < TimeSpan.FromSeconds(10))
    await Task.Delay(100);

if (!client.IsReady)
{
    Console.WriteLine("Ruleset çekilemedi — backend çalışıyor mu, anahtarlar doğru mu?");
    await host.StopAsync();
    return;
}

Console.WriteLine($"Hazır. Cache'deki flag sayısı: {client.Current?.Flags.Count ?? 0}\n");

// ──────────────────────────────────────────────────────────────────────────
// Test 1: Distribution + conversion simulation.
// 2000 user'ın her birine variant ata; sonra synthetic conversion rate'le
// (eski %10, yeni %15) checkout_completed event'i tetikle. Bu sayede dashboard
// "hangi variant daha başarılı" sorusunu gerçek veriyle gösterebilecek.
// ──────────────────────────────────────────────────────────────────────────
const int N = 2000;
const double conversionRateEski = 0.10;   // 10%
const double conversionRateYeni = 0.15;   // 15%

Console.WriteLine($"=== Test 1: {N} user için variant + conversion simülasyonu ===");

var histogram = new Dictionary<string, int>();
var convertedHistogram = new Dictionary<string, int>();
var rng = new Random();

for (var i = 0; i < N; i++)
{
    var userKey = $"{userPrefix}_{i}";
    var result = client.GetVariant("fiyat-listesi", userKey);
    var variant = result.VariantKey ?? "(off)";
    histogram[variant] = histogram.GetValueOrDefault(variant) + 1;

    var rate = variant switch
    {
        "yeni" => conversionRateYeni,
        "eski" => conversionRateEski,
        _ => 0.0
    };

    if (rate > 0 && rng.NextDouble() < rate)
    {
        // Revenue: 200-300 TL aralığında uniform random.
        var revenue = 200m + (decimal)(rng.NextDouble() * 100);
        client.Track("checkout_completed", userKey, revenue);
        convertedHistogram[variant] = convertedHistogram.GetValueOrDefault(variant) + 1;
    }
}

Console.WriteLine("\nVariant dağılımı:");
foreach (var (variant, count) in histogram.OrderByDescending(kv => kv.Value))
    Console.WriteLine($"  {variant,-12} {count,5}  ({100.0 * count / N,5:F2}%)");

Console.WriteLine("\nConversion (synthetic):");
foreach (var (variant, count) in convertedHistogram.OrderByDescending(kv => kv.Value))
{
    var exposed = histogram[variant];
    var actualRate = 100.0 * count / exposed;
    Console.WriteLine($"  {variant,-12} {count,4}/{exposed,-4} converted  ({actualRate:F2}%)");
}

// ──────────────────────────────────────────────────────────────────────────
// Test 2: Stable bucketing — aynı user her zaman aynı variant.
// ──────────────────────────────────────────────────────────────────────────
Console.WriteLine("\n=== Test 2: Stability (aynı user → aynı variant) ===");
foreach (var user in new[] { $"{userPrefix}_42", $"{userPrefix}_99", $"{userPrefix}_777" })
{
    var calls = Enumerable.Range(0, 5)
        .Select(_ => client.GetVariant("fiyat-listesi", user).VariantKey)
        .ToList();
    var consistent = calls.All(v => v == calls[0]);
    Console.WriteLine($"  {user,-10} → {calls[0],-6}  (5 çağrı hepsi aynı: {consistent})");
}

// ──────────────────────────────────────────────────────────────────────────
// Test 3: Dedup — Test 2'deki 3 user × 5 çağrı = 15 çağrı, ama dedup yüzünden
// queue'ya sadece 3 event girmiş olmalı (variant key her seferinde aynı).
// Bu Test 3 doğrulaması DB'den yapılır.
// ──────────────────────────────────────────────────────────────────────────

// Flusher'ın çalışması için bekle. FlushInterval=2s × birkaç tick + buffer.
Console.WriteLine("\n=== Background flush bekleniyor (8sn) ===");
await Task.Delay(TimeSpan.FromSeconds(8));

Console.WriteLine(@"
Bitti. DB'de doğrulamak için:

  docker exec postgres_switchly_db psql -U test_user -d switchly -c ""
    SELECT v.\""Key\"" AS variant, COUNT(DISTINCT e.\""UserKey\"") AS users
    FROM \""FlagExposureEvents\"" e
    JOIN \""Variants\"" v ON v.\""Id\"" = e.\""VariantId\""
    JOIN \""FeatureFlags\"" f ON f.\""Id\"" = e.\""FeatureFlagId\""
    WHERE f.\""Key\"" = 'fiyat-listesi'
    GROUP BY v.\""Key\"";""

Beklenen: variant başına ~1000 unique user.
");

await host.StopAsync();
