using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Switchly.Sdk.Internal;

namespace Switchly.Sdk;

/// <summary>
/// Public Switchly client — sync feature-flag evaluation against an in-memory ruleset
/// that is kept fresh by a background refresher.
///
/// Konsumer DI'dan tek bir instance alır (singleton); flag check'leri lock-free,
/// network-free, mikrosaniye seviyesinde döner.
/// </summary>
public sealed class SwitchlyClient
{
    private readonly RulesetCache _cache;
    private readonly FlagEvaluator _evaluator;
    private readonly ExposureRecorder _recorder;
    private readonly ConversionRecorder _conversionRecorder;
    private readonly IServiceScopeFactory _scopeFactory;

    internal SwitchlyClient(
        RulesetCache cache,
        FlagEvaluator evaluator,
        ExposureRecorder recorder,
        ConversionRecorder conversionRecorder,
        IServiceScopeFactory scopeFactory)
    {
        _cache = cache;
        _evaluator = evaluator;
        _recorder = recorder;
        _conversionRecorder = conversionRecorder;
        _scopeFactory = scopeFactory;
    }

    /// <summary>İlk fetch tamamlandı mı? Tamamlanana kadar IsOn safe-default (false) döner.</summary>
    public bool IsReady => _cache.IsReady;

    /// <summary>Şu an cache'lenmiş ruleset (debug/inspection için).</summary>
    public Ruleset? Current => _cache.Current;

    /// <summary>
    /// Verilen flag'in bu user için açık olup olmadığını local'de değerlendirir.
    /// Network çağrısı yok; cache henüz dolmadıysa veya flag bulunamazsa false döner.
    /// Multivariant flag'lerde "bir variant atandı mı?" anlamına gelir; hangi variant
    /// olduğunu öğrenmek için <see cref="GetVariant"/> kullan.
    /// </summary>
    /// <param name="flagKey">Flag'in slug-style key'i (Id değil).</param>
    /// <param name="userKey">User'ı tanımlayan stable string. Percentage/variant rollout için ŞART
    /// — aynı user her zaman aynı bucket'a düşer. AllUsers/Off rollout için gereksiz.</param>
    /// <param name="traits">Segment rule eşleştirmesi için key/value attribute'ları (örn. country, plan).</param>
    public bool IsOn(
        string flagKey,
        string? userKey = null,
        IReadOnlyDictionary<string, string>? traits = null)
        => GetVariant(flagKey, userKey, traits).IsOn;

    /// <summary>
    /// Verilen flag için bu user'a hangi variant atandığını döner.
    /// Boolean flag'de VariantKey null; sadece <see cref="EvaluationResult.IsOn"/> anlamlı.
    /// Multivariant flag'de IsOn=true ise VariantKey ve (varsa) PayloadJson dolu döner.
    /// </summary>
    public EvaluationResult GetVariant(
        string flagKey,
        string? userKey = null,
        IReadOnlyDictionary<string, string>? traits = null)
    {
        var ruleset = _cache.Current;
        if (ruleset is null) return EvaluationResult.Off;

        var flag = ruleset.Flags.FirstOrDefault(f => f.Key == flagKey);
        if (flag is null) return EvaluationResult.Off;

        var result = _evaluator.Evaluate(flag, userKey, traits);

        // Exposure tracking — sadece flag gerçekten evaluate edildiğinde kaydet.
        // Ruleset yüklenmediği veya flag bulunmadığı durumlarda Off dönüyoruz ama
        // bunlar consumer'ın yanlış konfig'i ya da SDK warmup state'i — tracking spam'lemesin.
        _recorder.TryRecord(userKey, flagKey, result.VariantKey, result.IsOn);

        return result;
    }

    /// <summary>
    /// Domain conversion event'i kaydeder: kullanıcı bizim için anlamlı bir eylem yaptı
    /// (checkout, signup, subscribe, vs). Backend analytics sorgusu bu olayı user'ın gördüğü
    /// son variant'a atfedip "hangi variant daha başarılı" sorusunu cevaplar.
    /// </summary>
    /// <param name="eventName">Domain event ismi — "checkout_completed", "signup", "button_clicked".
    /// Stable identifier olmalı; bir kez kararlaştır, sonra dashboard'da raporlar bu key'le çıkar.</param>
    /// <param name="userKey">User'ın stable identifier'ı. Exposure'da kullanılanla AYNI olmalı —
    /// attribution bu key üzerinden join'leniyor.</param>
    /// <param name="value">Opsiyonel numeric metric: revenue, score, sayım. null = sadece sayım eventi.</param>
    /// <param name="properties">Opsiyonel metadata (filter/drill-down için). JSON'a serialize edilip backend'e yollanır.</param>
    public void Track(
        string eventName,
        string userKey,
        decimal? value = null,
        IReadOnlyDictionary<string, string>? properties = null)
    {
        string? propsJson = null;
        if (properties is { Count: > 0 })
        {
            try
            {
                propsJson = JsonSerializer.Serialize(properties);
            }
            catch
            {
                // Serialization patladıysa event'i tamamen düşürmeyelim, sadece properties kaybolsun.
                propsJson = null;
            }
        }

        _conversionRecorder.TryRecord(userKey, eventName, value, propsJson);
    }

    /// <summary>
    /// Background refresh'i beklemeden manuel olarak ruleset'i yeniden çeker ve cache'i günceller.
    /// Test ve "şimdi senkronize ol" senaryoları için.
    /// </summary>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var fetcher = scope.ServiceProvider.GetRequiredService<RulesetFetcher>();
        var ruleset = await fetcher.FetchAsync(ct);
        _cache.Set(ruleset);
    }
}
