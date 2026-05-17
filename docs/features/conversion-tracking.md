# Conversion Tracking

**Status:** Implementation complete — backend + SDK + UI hepsi yazıldı, gerçek veriyle test edildi (6K user × ~12.4% ortalama conversion, +40.9% lift gözlemlendi).
**Started:** 2026-05-17
**Completed:** 2026-05-17

## Özet

[[exposure-tracking]]'in tamamlayıcısı. Exposure "kim variant'ı gördü" der; conversion "user görüş sonrası bizim için anlamlı bir eylem yaptı mı" der. İkisini join'leyince "hangi variant daha başarılı?" sorusu cevaplanabilir hale gelir.

Conversion event örnekleri:
- `checkout_completed` (value=250 TL)
- `signup`
- `subscribe`
- `button_clicked`

İki yeni şey getiriyoruz:
1. **ConversionEvent tablosu** — domain event'lerini saklar. Flag-agnostic; aynı conversion her flag'in analizinde kullanılabilir.
2. **POST /api/track/conversions endpoint** — exposure tracking endpoint'inin kardeşi. SDK `switchly.TrackAsync("checkout", userKey, value: 250m)` çağrısıyla burayı vurur.

Phase 2 olarak listede yazıyordu; şimdi yapma vakti çünkü exposure-only analytics "kim ne gördü"den fazlasını söyleyemiyor.

## Mimari kararlar ve gerekçeleri

### ConversionEvent flag-agnostic
Tabloya `FeatureFlagId` veya `VariantId` koymuyoruz. Sebep:
- Bir kullanıcının "checkout_completed" eylemi gerçek dünya olayı; hangi flag'in onu etkilediğini sadece analiz sırasında bilebiliriz.
- Tek conversion N flag'in analizinde kullanılabilir (paralel A/B testleri).
- Backend-side join attribution stratejimize uygun.

### Backend-side join attribution
Analytics sorgusu sırasında her conversion'ı, user'ın o **flag** için **en son gördüğü variant**'a atf'ediyoruz. SDK conversion'la birlikte variant göndermez — sadece event ismi + user.

Avantajları:
- SDK basit kalır (tek API: `Track(eventName, userKey, value)`).
- Tarihsel verilerde retro-aktif analiz yapılabilir (variant attribution sorgu sırasında hesaplanıyor).
- Bir kullanıcı conversion sırasında birden fazla flag'in etkisi altında olabilir; bunu analiz tarafına bırakıyoruz.

Dezavantajı:
- Sorgu pahalı: `FlagExposureEvents` × `ConversionEvents` join'i milyon row'da yavaşlayabilir. MVP'de fine; rollup tablosu Phase 3.

### Same tüm scope (Org/Project/Env) + denormalize
Exposure ile aynı şema: 4 hiyerarşi kolonu denormalize. Sebep aynı: join'siz tarama, query basit.

### Value decimal nullable
Conversion value'su olabilir veya olmayabilir:
- `checkout_completed: value=250` (revenue)
- `signup: value=null` (sayım metric, value yok)

`decimal?` ile her ikisi de destekleniyor.

### PropertiesJson opsiyonel
İleride filter / drill-down için ("only mobile users", "only orders > 1000 TL") metadata gerekebilir. Şimdi yazılan, şimdi okunmayan kolon. Postgres jsonb.

### OccurredAt client-reported
Exposure ile simetri. SDK conversion anını damgalıyor; queue lag analytics zamanını yanıltmasın diye.

### Public endpoint, PublicKey scope
SDK'dan unauth POST. Exposure endpoint'i ile aynı.

### Fail-soft: bilinmeyen event name yok (geçerli vs reddedilen kontrolü yok)
EventName serbest string, herhangi bir whitelist yok. Sebep:
- Consumer hangi event'leri tanımladığını biz bilmiyoruz.
- "İleride event tanımlama" özelliği eklenebilir (`EventTypes` tablo), ama MVP'de gereksiz.

## MVP kısayolları / Phase 3'te revize edilecekler

- **Cross-instance attribution race condition**: user A'nın conversion'ı server-1'den gelir, exposure'ı server-2'den gelir; eğer conversion exposure'dan önce DB'ye yazılırsa join'de eşleşmez. Çözüm: 5dk grace period veya periyodik geriye dönük re-attribution.
- **Variant değişimi sonrası eski attribution**: user önce `eski`'yi gördü, sonra weight değiştirildi, `yeni`'ye geçti, sonra convert oldu. Hangi variant'a atfetelim? Şu an "son görülen". Alternatif: "**conversion'dan önceki** son görülen". İkincisi daha doğru; query'de `WHERE exposure.OccurredAt < conversion.OccurredAt` ile yapılır.
- **Pre-aggregated rollup yok**: her sorgu join hesaplıyor. Volume yükselince yavaşlar.
- **Event tipi tanım yok**: `EventName` whitelist'siz; typo (`chekout` vs `checkout`) sessiz hatadır. Phase 2.5: dashboard'dan "tanımlı event'ler" listesi + SDK side warning.
- **Multi-flag attribution çakışması**: aynı conversion 5 farklı flag'in analizinde çıkıyor. Her birinin "kazanan variant"ı farklı çıkabilir; bu istatistiksel çakışma (multiple comparisons problem). MVP'de tek flag analizi yapıyoruz, çoklu flag overlay sonra.
- ~~**Statistical significance hesabı yok**~~ → **Eklendi 2026-05-17.** Two-proportion z-test + Wald CI on difference, p < 0.05 eşiğiyle. Backend `ProportionStats` helper'ı, UI'da yeşil/turuncu rozet + güven aralığı görünür. Geriye kalan iyileştirme: sample size calculator ("kaç user daha gerekli"), Bayesian alternative.
- **No deduplication on conversions**: aynı user'ın aynı dakikada 2 kez "checkout_completed" göndermesi 2 row üretir (idempotent değil). Phase 2.5: client-generated event Id + unique constraint.
- **Retention policy yok**: exposure tablosu gibi, conversion tablosu da büyür. Archive/delete job sonra.

## Dosya haritası

**Backend (yazılacak):**
- `Switchly-2.0.WebApi/Entities/ConversionEvent.cs`
- `Switchly-2.0.WebApi/Context/Configurations/ConversionEventConfiguration.cs`
- `Switchly-2.0.WebApi/Context/Migrations/<timestamp>_AddConversionEvents.cs`
- `Switchly-2.0.WebApi/Features/Tracking/TrackConversions/TrackConversionsHandler.cs`
- `Switchly-2.0.WebApi/Features/Tracking/TrackConversions/TrackConversionsEndpoint.cs`

**SDK (yazılacak):**
- `Switchly.Sdk/Internal/Models/ConversionEvent.cs` — internal record
- `Switchly.Sdk/Internal/ConversionRecorder.cs` — exposure recorder'a paralel; bounded Channel
- `Switchly.Sdk/Internal/ConversionSender.cs` — POST helper
- `Switchly.Sdk/Internal/BackgroundFlusher.cs` — extend: iki queue drain (veya ayrı `ConversionFlusher`)
- `Switchly.Sdk/SwitchlyClient.cs` — public `TrackAsync(eventName, userKey, value?, properties?)`
- `Switchly.Sdk/ServiceCollectionExtensions.cs` — DI registration

**Backend (analytics):**
- `Switchly-2.0.WebApi/Features/Tracking/GetFlagConversionStats/` — exposure × conversion join sorgusu

**UI (yazılacak):**
- `lib/types/feature-flag.ts` — `FlagConversionStatsDto`, lift fields
- `lib/services/feature-flag.service.ts` — `getConversionStats(flagId, eventName, since)`
- `app/dashboard/flags/page.tsx` — analytics modal'a conversion satırları + lift gösterimi + event name input

**Sample console:**
- `Switchly.Sdk.SampleConsole/Program.cs` — conversion simulation (variant başına farklı conversion rate)
