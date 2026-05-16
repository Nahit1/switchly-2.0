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
    private readonly IServiceScopeFactory _scopeFactory;

    internal SwitchlyClient(
        RulesetCache cache,
        FlagEvaluator evaluator,
        IServiceScopeFactory scopeFactory)
    {
        _cache = cache;
        _evaluator = evaluator;
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

        return _evaluator.Evaluate(flag, userKey, traits);
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
