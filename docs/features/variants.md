# Multivariant Flags & Variant Weights

**Status:** Complete (2026-05-11 migration tarihinden bu yana çalışıyor).
**Documented retroactively:** 2026-05-16

## Özet

Boolean flag (on/off) yetmediği senaryolar için **multivariant** flag desteği: bir flag'in N tane olası "çıktısı" olabiliyor (`control`, `blue`, `green`...), her variant kendi opsiyonel JSON payload'ını taşıyabiliyor (config flag use-case'i). User'lar variant'lara **stable bucketing** (SHA256(flagKey:userKey) % 100) ile dağıtılıyor; dağılım oranı **weight** olarak iki seviyede konfigüre ediliyor:

- **Environment-level default weights** — hiçbir segment targeting eşleşmediğinde uygulanır.
- **Targeting-level override weights** — belirli bir segment eşleştiğinde o targeting'in weight'i geçerli.

Evaluator (hem backend `EvaluateFlagHandler` hem SDK `FlagEvaluator`) variant seçimini `SortOrder`'a göre cumulative weight match'le yapıyor; iki taraf aynı algoritmayı kullanıyor, aynı user her zaman aynı variant'a düşüyor.

## Mimari kararlar ve gerekçeleri

### İki ayrı weight tablosu (env vs targeting), unified değil
`FeatureFlagEnvironmentVariantWeight` ve `FeatureFlagSegmentTargetingVariantWeight` ayrı tablolar. Tek tablo (`scope: env|targeting`) düşünüldü ama reddedildi:
- FK netliği: parent'a doğrudan cascade kuralı.
- Aynı flag-env içinde env-default + her targeting'in kendi weight'i koexist eder; tek tablo'da tuple-key karmaşıklaşırdı.
- Sorgu basitliği: evaluator iki ayrı navigation property'den okur.

### Weight: integer 0-100, toplam 100 (app-side validate)
DB-level CHECK constraint yok; validation `SetEnvironmentVariantWeightsHandler` ve `SetTargetingVariantWeightsHandler` içinde. Sebep:
- DB constraint replace-all sırasında ara state'i (silindi, henüz eklenmedi) yakalayıp false positive verir.
- App-side validation hata mesajını user-friendly döner (`Response<T>.Fail(...)`).
- Decimal weight (0-1.0 veya 0-10000) düşünüldü; integer 0-100 dashboard UX'i için en doğal — slider hızlı algılanıyor.

### Replace-all weight set stratejisi (diff yok)
`SetWeights` çağrısı mevcut weight'leri **siler, yenilerini ekler**. Diff hesaplama (which to update/insert/delete) atlandı çünkü:
- Variant sayısı küçük (genelde 2-5), N ufak.
- Diff bug'larından kaçınmak için "saf replace" daha güvenli.
- Toplam=100 garantisi tek atomik işlemde sağlanıyor.

### Variant key stable, immutable on update
`UpdateVariantHandler` sadece `Name` ve `PayloadJson` günceller — `Key` değiştirilemez. Sebep:
- Analytics ve log'larda variant key stable identifier; runtime'da değişirse historical data ile cross-reference kırılır.
- Rename istenirse delete + add önerilir (handler yorumunda da yazıyor).

### SortOrder-based deterministic variant ordering
Bucket'a göre variant seçilirken variants `SortOrder` ASC'de iterate edilir, cumulative weight 100'e kadar artar. Sebep:
- Server-side ve SDK-side evaluator **aynı order**'da bucket'lasın → tutarlı sonuç. Order farklılaşırsa aynı user iki tarafta farklı variant alır → felaket.
- `SortOrder` UI'da drag-drop yeniden düzenleme ile yönetilebilir.

### SHA256(flagKey:userKey) bucketing — boolean ile aynı
Multivariant variant seçimi için kullanılan bucket fonksiyonu Boolean rollout için kullanılan **birebir aynı**. Trade-off:
- Avantaj: bir kullanıcı boolean'dan multivariant'a geçirilen flag için bucket'ı **aynı yerde** kalır — yani sticky.
- Risk: aynı user farklı flag'lerde **bağımsız** bucket'lanır (flagKey salt görevi görüyor) — beklenen davranış.

### Variant en az 2 invariant'ı (multivariant'ta)
`DeleteVariantHandler` son ikisini sildirtmiyor. Sebep: multivariant'ın anlamı en az 2 alternatif. 1 variant kalırsa flag pratikte boolean'a indirgenir; kullanıcı yanlışlıkla bunu yapmasın diye explicit guard.

### Variant silmek cascade — weight row'ları düşer
Variant silindiğinde `FK Variants -> Weights` cascade davranışıyla weight kayıtları da düşer. Sonuç: sum<100 kalabilir. Evaluator missing variant'ı 0 weight gibi davranıyor (lookup'ta `TryGetValue` miss → continue). Bu davranış handler'da yorum satırıyla belgelenmiş; kullanıcı silme sonrası weight'leri tekrar set etmek zorunda.

### Baseline weight on flag create: ilk variant 100, kalan 0 (sadece default env'de)
`CreateFlagCommandHandler` multivariant flag oluştururken **default environment**'a 100/0/0 weight'leri otomatik ekliyor. Sebep:
- Boolean'daki "default env AllUsers/100" pattern'inin multivariant karşılığı.
- Flag yaratılır yaratılmaz "weight tanımsız" state'ine düşmesin.
- Default env dışı env'lere (staging, prod) weight ayrı set edilir — promotion explicit.

### PayloadJson string olarak saklı, jsonb mapleniyor
`Variant.PayloadJson` C# tarafında `string?`. Npgsql `jsonb` kolonuna map ediliyor (default).
- Validation yok (serbest JSON); validation eklenirse Variant per-flag schema gerektirir, Phase 2.
- "Config flag" use-case'i bu kolonla karşılanıyor (variant = config preset).

### Snapshot projection: tek query'de tüm eval verisi
`EvaluateFlagHandler` flag, env state, variants, env-weights, targetings, targeting-weights, segment rules — hepsini **tek EF Core projection query**'sinde çekiyor. N+1 yok, latency düşük. SDK tarafı için `GetRulesetHandler` aynı pattern'i tüm flag'ler için yapıyor.

### Boolean flag'ler için variant tablosu boş, davranış branch'leniyor
Variant entity boolean flag'lerde **hiç row üretmiyor**. Evaluator `flag.Type == Multivariant` check'iyle dallanıyor:
- Multivariant → `PickVariant(weights, userKey)`
- Boolean → `Boolean(ApplyRollout(rolloutKind, percentage, ...))`

Tek model, iki davranış.

### SDK-side evaluator: server logic'in birebir port'u
`Switchly.Sdk/Internal/FlagEvaluator.cs` backend'deki `EvaluateFlagHandler.Evaluate`'in mirror'ı. Aynı SHA256, aynı SortOrder iteration, aynı cumulative weight match. İki tarafın **algoritmik divergence**'ı olmaması şart — yoksa same user farklı sonuç alır.

## MVP kısayolları / Phase 2'de revize edilecekler

- **Weight için decimal/percentage precision yok**: integer 0-100. Bazı use-case'lerde %0.5 weight (canary release) gerekebilir; bu durumda integer 0-10000 (basis points) veya decimal'a geçilir. Migration breaking.
- **Bucket salt sadece flagKey**: aynı user iki farklı flag'de bağımsız bucket'a düşüyor — doğru, ama bir flag'in *yeniden randomize* edilmesi için fonksiyonun salt'lanması (versioning) lazım. Şu an yok; weight değiştirilse bile aynı user'lar aynı bucket'ta kalır.
- **Variant payload validation yok**: serbest JSON. JSON schema, type validation, payload size limit yok. Phase 2'de `Variant.PayloadSchema` (per-flag) eklenebilir.
- **Variant-level audit log yok**: kim ne zaman weight'i değiştirdi, hangi variant ne zaman silindi — kayıt yok. Production'da gerekecek; ayrı audit tablosu veya event sourcing.
- **Bucket flag-level, env-specific değil**: aynı user dev'de blue, prod'da blue. Genelde istenen davranış ama experiment isolation gerekirse env salt eklemek gerekir.
- **Targeting weight conflict resolution yok**: birden fazla targeting eşleşirse priority desc'te ilk match alınıyor. "Eşit priority'de tie-break" davranışı belirsiz — DB sort order'a düşüyor. Phase 2'de explicit deterministic tie-break.
- **Variant key case duality**: stored case-sensitive (`v.Key.Trim()`), uniqueness check case-insensitive (`OrdinalIgnoreCase`). Tutarsız; lookup'larda lowercase normalize ediyoruz ama bu da ekstra mantık. Çözüm: storage'a lowercase normalize.
- **Weight reset on variant delete**: variant silinince kalan weight'ler otomatik rebalance edilmiyor. Sum<100 olabilir, evaluator tolere ediyor ama UI'da kullanıcıya hatırlatmak gerek. Otomatik rebalance (kalan variant'lara orantılı dağıt) düşünülebilir.
- **No "experiment" concept**: variant ↔ outcome bağlantısı belirsiz. Phase 2'de "Experiment" entity'si gelirse (start_time, end_time, hypothesis, primary_metric), variant weight'leri ona bağlanır; analytics conversion attribution buradan ölçülür.
- **No traffic split preview**: weight değiştirilince "kaç user etkilenecek" önizlemesi yok. Mevcut bucket dağılımını gösteren dashboard widget'ı Phase 2.
- **Variant rename = delete+add**: kullanıcı rename istiyorsa data integrity kaybediyor. Audit-friendly rename (key history) gerekirse `VariantKeyHistory` tablosu.
- **No multivariant boolean fallback**: multivariant flag'de hiçbir variant eşleşmezse `Off` döner (`IsOn=false`, `VariantKey=null`). Bu durumda consumer "default behavior" karar veriyor — bilinçli ama kullanıcı dokümante edilmedi.

## Dosya haritası

**Backend entity & schema:**
- `Switchly-2.0.WebApi/Entities/Variant.cs`
- `Switchly-2.0.WebApi/Entities/FeatureFlagEnvironmentVariantWeight.cs`
- `Switchly-2.0.WebApi/Entities/FeatureFlagSegmentTargetingVariantWeight.cs`
- `Switchly-2.0.WebApi/Entities/FeatureFlagEnvironment.cs` (VariantWeights navigation)
- `Switchly-2.0.WebApi/Entities/FeatureFlagSegmentTargeting.cs` (VariantWeights navigation)
- `Switchly-2.0.WebApi/Context/Configurations/FeatureFlagEnvironmentVariantWeightConfiguration.cs`
- `Switchly-2.0.WebApi/Context/Configurations/FeatureFlagSegmentTargetingVariantWeightConfiguration.cs`
- `Switchly-2.0.WebApi/Context/Migrations/20260511090524_AddVariantWeightTables.cs`

**Backend handlers/endpoints:**
- `Switchly-2.0.WebApi/Features/Flags/CreateFlag/CreateFlagCommandHandler.cs` (variant input + baseline weight)
- `Switchly-2.0.WebApi/Features/Variants/AddVariant/` (Add)
- `Switchly-2.0.WebApi/Features/Variants/UpdateVariant/` (Update, key immutable)
- `Switchly-2.0.WebApi/Features/Variants/DeleteVariant/` (Delete, min 2 guard)
- `Switchly-2.0.WebApi/Features/ProjectEnviroments/SetEnvironmentVariantWeights/`
- `Switchly-2.0.WebApi/Features/ProjectEnviroments/SetTargetingVariantWeights/`
- `Switchly-2.0.WebApi/Features/Flags/EvaluateFlag/EvaluateFlagHandler.cs` (PickVariant)
- `Switchly-2.0.WebApi/Features/Flags/GetRuleset/GetRulesetHandler.cs` (variant + weights in DTO)

**SDK:**
- `Switchly.Sdk/Internal/FlagEvaluator.cs` (PickVariant — backend mirror)
- `Switchly.Sdk/Internal/Models/Ruleset.cs` (FlagDefinition, VariantInfo, VariantWeight, Targeting)
- `Switchly.Sdk/Internal/Models/FeatureFlagType.cs`
- `Switchly.Sdk/EvaluationResult.cs` (IsOn, VariantKey, PayloadJson struct)
- `Switchly.Sdk/SwitchlyClient.cs` (IsOn, GetVariant)
