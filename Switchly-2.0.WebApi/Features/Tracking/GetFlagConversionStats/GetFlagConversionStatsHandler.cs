using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Observability;

namespace Switchly_2._0.WebApi.Features.Tracking.GetFlagConversionStats;

public sealed record GetFlagConversionStatsQuery(
    Guid FlagId,
    string EventName,
    Guid? EnvironmentId,
    DateTimeOffset Since
) : IRequest<Response<FlagConversionStatsDto>>;

public sealed record FlagConversionStatsDto(
    Guid FlagId,
    string FlagKey,
    string EventName,
    DateTimeOffset Since,
    int TotalExposedUsers,
    int TotalConvertedUsers,
    decimal TotalValue,
    List<VariantConversionStatsDto> Variants
);

public sealed record VariantConversionStatsDto(
    Guid? VariantId,
    string? VariantKey,
    bool IsOn,
    int ExposedUsers,
    int ConvertedUsers,
    decimal TotalValue,
    double? PValue,                  // Baseline'da veya hesap yapılamıyorsa null.
    bool IsSignificant,              // p < 0.05 → true. Baseline'da false.
    double? LiftPercent,             // Baseline'a göre lift % (display için backend hesaplıyor).
    double? LiftCiLowPercent,        // Lift güven aralığının alt sınırı (%95 güven).
    double? LiftCiHighPercent,       // Üst sınır.
    bool IsBaseline,                 // Bu satır baseline mı (UI badge için).
    int? RequiredAdditionalUsers     // Significance'a ulaşmak için kaç user daha gerek (variant başına). null = baseline veya zaten anlamlı.
);

// Exposure × Conversion join: user'ın last variant'ı → conversion'lar.
// MVP'de in-memory aggregation (2 query, sonra LINQ to Objects). Volume büyürse
// raw SQL DISTINCT ON veya pre-aggregated rollup gerekecek.
public sealed class GetFlagConversionStatsHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<GetFlagConversionStatsQuery, Response<FlagConversionStatsDto>>
{
    public async Task<Response<FlagConversionStatsDto>> Handle(GetFlagConversionStatsQuery request, CancellationToken ct)
    {
        using var activity = SwitchlyActivitySources.Tracking.StartActivity("GetFlagConversionStats.Join");
        activity?.SetTag("flag_id", request.FlagId.ToString());
        activity?.SetTag("event_name", request.EventName);

        var eventName = (request.EventName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(eventName))
            return Response<FlagConversionStatsDto>.Fail("EventName zorunludur.");

        // Flag + auth.
        var flag = await context.FeatureFlags
            .AsNoTracking()
            .Where(f => f.Id == request.FlagId)
            .Select(f => new { f.Id, f.Key, f.Project.OrganizationId })
            .FirstOrDefaultAsync(ct);

        if (flag is null)
            return Response<FlagConversionStatsDto>.Fail("Flag bulunamadı.");

        var isMember = await context.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == flag.OrganizationId
                           && m.UserId == userContext.UserId, ct);

        if (!isMember)
            throw new UnauthorizedAccessException("Bu organization için yetkin yok.");

        // Step 1: zaman penceresindeki tüm exposure'ları çek (env filter opsiyonel).
        var exposuresQuery = context.FlagExposureEvents
            .AsNoTracking()
            .Where(e => e.FeatureFlagId == request.FlagId && e.OccurredAt >= request.Since);

        if (request.EnvironmentId.HasValue)
            exposuresQuery = exposuresQuery.Where(e => e.ProjectEnvironmentId == request.EnvironmentId.Value);

        var exposures = await exposuresQuery
            .Select(e => new ExposureRow(e.UserKey, e.VariantId, e.IsOn, e.OccurredAt))
            .ToListAsync(ct);

        if (exposures.Count == 0)
        {
            return Response<FlagConversionStatsDto>.Ok(new FlagConversionStatsDto(
                flag.Id, flag.Key, eventName, request.Since, 0, 0, 0m,
                new List<VariantConversionStatsDto>()));
        }

        // In-memory: user başına LAST exposure. (DISTINCT ON Postgres-spesifik; in-memory daha portable.)
        // Variant değişimi durumunda son seen attribution uygulanır.
        var userLatest = exposures
            .GroupBy(e => e.UserKey)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(e => e.OccurredAt).First()
            );

        // Step 2: ilgili user'ların hedef event'leri (env filter opsiyonel).
        var userKeys = userLatest.Keys.ToList();
        var conversionsQuery = context.ConversionEvents
            .AsNoTracking()
            .Where(c => c.EventName == eventName
                        && c.OccurredAt >= request.Since
                        && userKeys.Contains(c.UserKey));

        if (request.EnvironmentId.HasValue)
            conversionsQuery = conversionsQuery.Where(c => c.ProjectEnvironmentId == request.EnvironmentId.Value);

        var conversions = await conversionsQuery
            .Select(c => new { c.UserKey, c.Value, c.OccurredAt })
            .ToListAsync(ct);

        // Variant başına: exposed user listesi + converted user set + total value.
        // Outcome key tuple = (VariantId, IsOn) — exposure stats ile aynı agg birimi.
        var byVariantExposed = userLatest.Values
            .GroupBy(e => new { e.VariantId, e.IsOn })
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.UserKey).Distinct().Count()
            );

        var variantConverters = new Dictionary<(Guid? VariantId, bool IsOn), HashSet<string>>();
        var variantTotalValue = new Dictionary<(Guid? VariantId, bool IsOn), decimal>();

        foreach (var c in conversions)
        {
            if (!userLatest.TryGetValue(c.UserKey, out var exposure)) continue;
            // Conversion exposure'dan ÖNCE olduysa atfetme — variant'ı görmeden önceki conversion sayılmaz.
            if (c.OccurredAt < exposure.OccurredAt) continue;

            var key = (exposure.VariantId, exposure.IsOn);
            if (!variantConverters.TryGetValue(key, out var set))
            {
                set = new HashSet<string>();
                variantConverters[key] = set;
            }
            set.Add(c.UserKey);
            variantTotalValue.TryGetValue(key, out var existing);
            variantTotalValue[key] = existing + (c.Value ?? 0m);
        }

        // Variant key'leri resolve et.
        var variantIds = byVariantExposed.Keys
            .Where(k => k.VariantId.HasValue)
            .Select(k => k.VariantId!.Value)
            .Distinct()
            .ToList();

        var variantKeyMap = variantIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await context.Variants
                .AsNoTracking()
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => v.Key, ct);

        // İlk pass: temel veriler (stats hesaplamadan).
        var rawVariants = byVariantExposed
            .Select(kv =>
            {
                var key = kv.Key;
                var converters = variantConverters.GetValueOrDefault((key.VariantId, key.IsOn), new HashSet<string>());
                var totalValue = variantTotalValue.GetValueOrDefault((key.VariantId, key.IsOn), 0m);
                string? variantKey = null;
                if (key.VariantId.HasValue && variantKeyMap.TryGetValue(key.VariantId.Value, out var resolvedKey))
                    variantKey = resolvedKey;
                var outcomeLabel = variantKey ?? (key.IsOn ? "on" : "off");
                return new
                {
                    VariantId = key.VariantId,
                    VariantKey = variantKey,
                    IsOn = key.IsOn,
                    ExposedUsers = kv.Value,
                    ConvertedUsers = converters.Count,
                    TotalValue = totalValue,
                    OutcomeLabel = outcomeLabel
                };
            })
            .ToList();

        // Baseline pick: alfabetik en küçük outcome label (UI ile aynı kural).
        // İstatistik karşılaştırmalar buna göre yapılır.
        var baselineRow = rawVariants
            .OrderBy(v => v.OutcomeLabel, StringComparer.Ordinal)
            .FirstOrDefault();

        // İkinci pass: stats ile zenginleştir.
        var variantDtos = rawVariants
            .Select(v =>
            {
                var isBaseline = baselineRow is not null
                    && v.VariantId == baselineRow.VariantId
                    && v.IsOn == baselineRow.IsOn;

                double? pValue = null;
                bool isSignificant = false;
                double? liftPercent = null;
                double? liftCiLow = null;
                double? liftCiHigh = null;
                int? requiredAdditionalUsers = null;

                if (!isBaseline && baselineRow is not null)
                {
                    var stats = ProportionStats.Compute(
                        conv1: v.ConvertedUsers, n1: v.ExposedUsers,
                        conv2: baselineRow.ConvertedUsers, n2: baselineRow.ExposedUsers);

                    if (stats is not null)
                    {
                        pValue = stats.PValue;
                        isSignificant = stats.PValue < 0.05;
                        liftPercent = stats.LiftPercent;
                        liftCiLow = stats.LiftCiLowPercent;
                        liftCiHigh = stats.LiftCiHighPercent;

                        // Anlamlı değilse: kaç user daha gerek?
                        if (!isSignificant)
                        {
                            var p1 = (double)v.ConvertedUsers / v.ExposedUsers;
                            var p2 = (double)baselineRow.ConvertedUsers / baselineRow.ExposedUsers;
                            var needed = ProportionStats.RequiredSampleSize(p1, p2);
                            if (needed.HasValue && needed.Value > v.ExposedUsers)
                                requiredAdditionalUsers = needed.Value - v.ExposedUsers;
                        }
                    }
                }

                return new VariantConversionStatsDto(
                    VariantId: v.VariantId,
                    VariantKey: v.VariantKey,
                    IsOn: v.IsOn,
                    ExposedUsers: v.ExposedUsers,
                    ConvertedUsers: v.ConvertedUsers,
                    TotalValue: v.TotalValue,
                    PValue: pValue,
                    IsSignificant: isSignificant,
                    LiftPercent: liftPercent,
                    LiftCiLowPercent: liftCiLow,
                    LiftCiHighPercent: liftCiHigh,
                    IsBaseline: isBaseline,
                    RequiredAdditionalUsers: requiredAdditionalUsers
                );
            })
            .OrderByDescending(v => v.ExposedUsers)
            .ThenBy(v => v.VariantKey)
            .ToList();

        var totalConverted = variantConverters.Values.SelectMany(s => s).Distinct().Count();
        var totalValueSum = variantTotalValue.Values.Sum();

        return Response<FlagConversionStatsDto>.Ok(new FlagConversionStatsDto(
            FlagId: flag.Id,
            FlagKey: flag.Key,
            EventName: eventName,
            Since: request.Since,
            TotalExposedUsers: userLatest.Count,
            TotalConvertedUsers: totalConverted,
            TotalValue: totalValueSum,
            Variants: variantDtos
        ));
    }

    private sealed record ExposureRow(string UserKey, Guid? VariantId, bool IsOn, DateTimeOffset OccurredAt);
}

/// <summary>
/// İki oran (proportion) arasındaki farkın istatistiksel anlamlılığını hesaplar.
/// İki ana çıktı:
///   1. p-value — "iki variant aslında aynı olsaydı, bu farkı şans eseri görme ihtimali".
///      p < 0.05 → konvansiyonel olarak "anlamlı".
///   2. Lift güven aralığı — gerçek lift'in büyük olasılıkla (%95) düştüğü alt/üst sınır.
/// Formül: two-proportion z-test (pooled), Wald CI on difference, then divided by baseline.
/// </summary>
internal static class ProportionStats
{
    public sealed record TestResult(
        double PValue,
        double LiftPercent,        // (p1 - p2) / p2 * 100
        double LiftCiLowPercent,
        double LiftCiHighPercent);

    /// <summary>
    /// Gözlenen p1/p2 farkını %95 güven + %80 power ile doğrulamak için variant başına
    /// kaç user gerekli? Klasik iki-orantı power analizi formülü:
    /// n = (z_{α/2}·√(2p̄q̄) + z_β·√(p1q1 + p2q2))² / (p1-p2)²
    ///     α=0.05 → z_{α/2}=1.96, power=0.80 → z_β=0.84.
    /// p1=p2 ise fark sıfır → sonsuz user gerekirdi, null döner.
    /// </summary>
    public static int? RequiredSampleSize(double p1, double p2)
    {
        if (p1 < 0 || p1 > 1 || p2 < 0 || p2 > 1) return null;
        var delta = p1 - p2;
        if (Math.Abs(delta) < 1e-9) return null;        // ölçülebilir fark yok

        const double zAlpha = 1.96;
        const double zBeta = 0.84;

        var pBar = (p1 + p2) / 2.0;
        var qBar = 1 - pBar;
        var q1 = 1 - p1;
        var q2 = 1 - p2;

        var numerator = zAlpha * Math.Sqrt(2 * pBar * qBar)
                      + zBeta * Math.Sqrt(p1 * q1 + p2 * q2);
        var n = (numerator * numerator) / (delta * delta);

        // Üst sınır koy: çok büyük rakamlar (örn. >10M) UI'da anlamsız.
        if (n > 10_000_000) return null;
        return (int)Math.Ceiling(n);
    }

    public static TestResult? Compute(int conv1, int n1, int conv2, int n2)
    {
        // Yeterli veri yoksa hesap yapma.
        if (n1 <= 0 || n2 <= 0) return null;

        var p1 = (double)conv1 / n1;
        var p2 = (double)conv2 / n2;

        // Baseline rate sıfırsa lift tanımsız (0'a bölme).
        if (p2 <= 0) return null;

        // p-value: two-proportion z-test, pooled variance.
        var pooled = (double)(conv1 + conv2) / (n1 + n2);
        var seZ = Math.Sqrt(pooled * (1 - pooled) * (1.0 / n1 + 1.0 / n2));

        double pValue;
        if (seZ <= 0)
        {
            // Edge case: ikisi de %0 ya da %100. Hesap yapamayız.
            pValue = 1.0;
        }
        else
        {
            var z = (p1 - p2) / seZ;
            pValue = 2.0 * (1.0 - NormalCdf(Math.Abs(z)));
        }

        // CI for difference p1-p2 (Wald): ±1.96 SE = %95 güven aralığı.
        var seDiff = Math.Sqrt(p1 * (1 - p1) / n1 + p2 * (1 - p2) / n2);
        var diff = p1 - p2;
        var diffLow = diff - 1.96 * seDiff;
        var diffHigh = diff + 1.96 * seDiff;

        // Lift = difference / baseline → percentage.
        var liftPercent = (diff / p2) * 100.0;
        var liftCiLow = (diffLow / p2) * 100.0;
        var liftCiHigh = (diffHigh / p2) * 100.0;

        return new TestResult(pValue, liftPercent, liftCiLow, liftCiHigh);
    }

    // Standart normal dağılım CDF: Φ(x) = P(Z ≤ x).
    private static double NormalCdf(double x)
        => 0.5 * (1.0 + Erf(x / Math.Sqrt(2.0)));

    // Abramowitz & Stegun 7.1.26 yaklaşım — max abs hata ~1.5e-7.
    // BCL'de erf yok; ufak polinom yaklaşımı yeterli, kütüphane ekleme gereksiz.
    private static double Erf(double x)
    {
        const double a1 = 0.254829592;
        const double a2 = -0.284496736;
        const double a3 = 1.421413741;
        const double a4 = -1.453152027;
        const double a5 = 1.061405429;
        const double p = 0.3275911;

        var sign = x < 0 ? -1.0 : 1.0;
        var ax = Math.Abs(x);
        var t = 1.0 / (1.0 + p * ax);
        var y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-ax * ax);
        return sign * y;
    }
}
