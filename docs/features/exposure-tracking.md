# Exposure Tracking

**Status:** Implementation complete — test edilmedi (console app ile distribution doğrulaması bekliyor).
**Started:** 2026-05-16
**Completed:** 2026-05-17

## Özet

SDK consumer'ın bir flag'i evaluate ettiği her benzersiz `(user, flag, variant)` üçlüsünü `FlagExposureEvents` tablosunda 1 satır olarak kaydetmek. Multivariant ve boolean flag'lerin her ikisi de destekleniyor. A/B test analitiğinin temeli; conversion event'leri (Phase 2) bu tablonun üzerine join'lenecek.

Mantık şöyle akıyor: consumer kodu `switchly.IsOn(...)`/`switchly.GetVariant(...)` çağırıyor → SDK lokalden eval yapıyor → eval sonucunu **de-dup cache + bounded queue** ile filtreleyip biriktiriyor → background flusher periyodik olarak batch'i backend'e POST'luyor → backend bulk insert'le tabloya yazıyor.

## Mimari kararlar ve gerekçeleri

### Storage: raw event log (pre-aggregate yok)
Her exposure ayrı satır. Pre-aggregated counter (örn. `(flagId, variantId, hour) → count`) düşünüldü ama reddedildi çünkü conversion attribution per-user join gerektiriyor — sayaç bunu mümkün kılmıyor. Volume büyürse rollup tablosu eklenir; baştan yapmak premature.

### Tek tablo, hem boolean hem multivariant
`VariantId` nullable. Boolean'da `null` + `IsOn=true/false`; multivariant atandı → dolu + `IsOn=true`; multivariant Off → `null` + `IsOn=false`. Tek şema iki use-case'i taşıyor.

### `IsOn=false` satırları da yazılıyor
Kontrol grubu (flag'i kapalı gören user'lar) analitik için gerekli. A/B test'in "etki yok"u ölçmek için her iki kovayı da bilmemiz lazım.

### Denormalize hiyerarşi (Org/Project/Env/Flag id'leri de kolonlarda)
Analytics sorguları her seferinde join etmek istemiyor. 4 ek Guid = 64 byte/row, query basit `WHERE` ile çözülüyor.

### Variant silinince exposure tarihçesi yaşar — FK `OnDelete(SetNull)`
"`user_42` blue gördü" bilgisi blue silinince anlamsızlaşmıyor, sadece pointer kopuyor. Variant'lar ephemeral, exposure history kalıcı.

### Org/Project/Env/Flag silinince cascade
Bu parent'lar silindiğinde event'ler orphan olmamalı; ayrıca org tamamen siliniyorsa privacy/cleanup gereği veriler düşmeli.

### `OccurredAt` = client-reported time (SDK eval anı), server ingestion değil
Queue lag (10sn) analytics zamanını yanıltmamalı. Clock drift riski var ama A/B test granülaritesinde önemsiz; gerekirse ayrıca `ReceivedAt` eklenir.

### Public endpoint, JWT yok
`PublicKey + ProjectKey + EnvironmentKey` ile org-scope. `/api/flag/evaluate` ile aynı felsefe — SDK consumer'ın login state'i yok.

### Backend fail-soft (bilinmeyen key'i skip et)
Eski ruleset'le çalışan SDK silinmiş flag yollayabilir. Tüm batch'i reject etmek yerine bilinmeyen flag → skip; bilinmeyen variant → `VariantId=null` yaz (event hâlâ değerli).

### Bulk resolve + bulk insert
500 event için 500 SELECT yapmıyoruz — flag key'leri Distinct edip tek IN query'siyle çekiyoruz. Insert tarafı EF 9 zaten batching yapıyor (`INSERT ... VALUES (...), (...), ...`).

### SDK de-dup: `(user, flag, variantKey ?? on/off)` + TTL 5dk
Aynı user aynı maruziyet için tekrar event üretmesin. TTL 5dk: web session granülaritesi; uzun tutsak variant değişimini geç yakalarız, kısa tutsak tablo şişer.

### SDK queue: `Channel<T>` bounded, `DropWrite`
Hot path bloklamasın. Queue dolarsa (10K event) yeni gelen drop edilir. Tracking ASLA `IsOn` çağrısını yavaşlatmamalı — flag check mikrosaniye işi.

### SDK crash'inde queue kaybı bilinçli
RAM-only queue, disk WAL yok. Tracking transactional değil; %0.01 veri kaybı flag check hot path'inde disk I/O'dan daha kabul edilebilir.

## MVP kısayolları / Phase 2'de revize edilecekler

- **Pre-aggregated rollup yok**: tablo 100M satıra ulaştığında dashboard sorguları yavaşlar. Çözüm: `mat-view` veya nightly rollup job `(flagId, variantId, hour) → count`.
- **Rate limiting yok**: `/api/track/exposures` public endpoint, kötü niyetli client spam'leyebilir. Çözüm: nginx layer veya `AspNetCoreRateLimit`.
- **Cross-instance de-dup yok**: 3 sunucuda çalışan consumer aynı user için 3 ayrı kez "ilk kez gördüm" diyebilir. Çözüm: Redis-shared cache veya backend-side idempotency key.
- **Idempotency yok**: aynı batch iki kez POST'lanırsa duplicate satır. Çözüm: client-generated event Id + unique index.
- **Disk-backed queue yok**: SDK process crash'inde son 10sn'lik event kaybolur. Çözüm: SQLite veya append-only file ile disk persistence (perf cost'a göre).
- **Conversion tracking yok**: A/B test'in lift hesabı için ConversionEvent tablosu + `switchly.TrackAsync(eventName, userKey, value)` API gerekiyor. Phase 2 first item.
- **TraitsJson kayıt yok**: hangi segment'e düştüğü/hangi trait'lerle eşleştiği saklanmıyor. Debug ve "neden bu user bu variant'a düştü" sorgusu için gerekirse opsiyonel kolon eklenir (PII farkındalıkla).
- **OpenTelemetry custom span'leri yok**: ingest endpoint'inde batch boyu metric'i ve latency ölçümü yok. Mevcut auto-instrumentation HTTP span'i veriyor; daha derin görünürlük için manuel span eklenir.
- **UserKey hashleme yok**: GDPR/PII için "hash before store" opsiyonu yok. Şu an raw saklanıyor; ileride `SwitchlyOptions.HashUserKey` flag'i eklenebilir.
- **Retention policy yok**: olay tablosu sınırsız büyüyor. 30/60/90 günden sonra archive/delete job gerekecek.
- **Bulk resolve case-sensitivity**: flag key tam eşleşme (Ordinal), variant key case-insensitive — biraz tutarsız, kararı tek yöne unify etmek lazım.
- **Flusher: tick başına tek batch**: queue backlog'u biriktiyse bir flush sadece FlushBatchSize event yolluyor, geri kalan sonraki tick'e kalıyor. Sürekli yüksek trafikte queue dolar → drop. Phase 2: tick başına N batch veya adaptif flush.
- **Flusher: retry/backoff yok**: POST patladığında batch düşer, requeue yok. Phase 2: exponential backoff + circuit breaker + local buffer ile yeniden deneme.
- **Final drain 5sn pencere**: shutdown'da kalan event'ler tek deneme, backend ulaşılamazsa gider. Phase 2: kısa local persistence (SQLite) ile cross-restart dayanım.

## Dosya haritası

**Backend:**
- `Switchly-2.0.WebApi/Entities/FlagExposureEvent.cs` — entity
- `Switchly-2.0.WebApi/Context/Configurations/FlagExposureEventConfiguration.cs` — index, FK delete behavior
- `Switchly-2.0.WebApi/Context/Migrations/20260516201639_AddFlagExposureEvents.cs` — schema
- `Switchly-2.0.WebApi/Features/Tracking/TrackExposures/TrackExposuresHandler.cs` — komut + handler
- `Switchly-2.0.WebApi/Features/Tracking/TrackExposures/TrackExposuresEndpoint.cs` — Carter route

**SDK:**
- `Switchly.Sdk/Internal/Models/ExposureEvent.cs` — internal record (queue payload)
- `Switchly.Sdk/Internal/ExposureRecorder.cs` — MemoryCache dedup + Channel producer; `TryRecord(...)` hot path
- `Switchly.Sdk/Internal/ExposureSender.cs` — HttpClient POST wrapper; `RulesetFetcher` simetriği
- `Switchly.Sdk/Internal/BackgroundFlusher.cs` — IHostedService; `PeriodicTimer` drain → POST + shutdown drain
- `Switchly.Sdk/SwitchlyClient.cs` — GetVariant içinde `recorder.TryRecord(...)` çağrısı (IsOn da geçiyor)
- `Switchly.Sdk/ServiceCollectionExtensions.cs` — Recorder singleton, Sender transient, Flusher hosted service kayıtları
- `Switchly.Sdk/SwitchlyOptions.cs` — TrackingEnabled, FlushInterval, FlushBatchSize, MaxQueueSize, DedupTtl
- `Switchly.Sdk/SwitchlyDefaults.cs` — default sabitler
- `Switchly.Sdk/Switchly.Sdk.csproj` — `Microsoft.Extensions.Caching.Memory` paketi
