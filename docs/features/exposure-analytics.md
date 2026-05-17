# Exposure Analytics (Read API + Dashboard)

**Status:** Implementation complete — UI'da görsel test bekleniyor.
**Started:** 2026-05-17
**Completed:** 2026-05-17

## Özet

[[exposure-tracking]] tarafından `FlagExposureEvents` tablosuna yazılan ham veriyi okunabilir analytics'e çevirme katmanı. İki bileşen:

1. **Backend read endpoint** — `GET /api/flag/{flagId}/exposure-stats` — flag için verilen zaman aralığında variant başına unique user + total exposure aggregation'ı döner.
2. **UI modal** — flag listesinde flag kartından açılan, distribution'ı tablo + CSS bar chart olarak gösteren modal.

Yazma path'i (SDK → tracking endpoint → DB) zaten kurulu. Bu katman sadece **okuma**: dashboard kullanıcısı "fiyat-listesi flag'i son 24 saatte hangi variant'a kaç user atfetti?" sorusunu cevaplayabilecek.

## Mimari kararlar ve gerekçeleri

### Real-time aggregation (cache yok, pre-aggregate yok)
Sorgu DB'ye direkt vurur. Sebep:
- MVP volume düşük (1-100K row/flag); Postgres index'lerle saniyenin altında cevaplar.
- Caching katmanı önemli kompleksite getirir (invalidation, TTL); henüz gerek yok.
- Pre-aggregated rollup tablosu Phase 2'de eklenebilir (`exposure-tracking.md`'de zaten borç olarak yazılı).

### JWT-protected, organizasyon üyeliği kontrolü
Dashboard private; user JWT ile login olur. Handler `OrganizationMembers` join'iyle flag'in user'ın org'una ait olduğunu doğrular. `EvaluateFlag`/`Tracking` endpoint'leri public ama bu öyle değil.

### Variant key + isOn ikilisi sorguda
`GROUP BY VariantId, IsOn` ile dört "outcome kovası" çıkar:
- `VariantId=dolu, IsOn=true` → multivariant'ta variant atandı.
- `VariantId=null, IsOn=true` → boolean flag açık görüldü.
- `VariantId=null, IsOn=false` → boolean flag kapalı veya multivariant Off.
- `VariantId=dolu, IsOn=false` → teoride olmamalı (yazmıyoruz), pratikte sıfır.

UI bu kovaları kullanıcıya anlamlı label'larla gösterir (örn. "Off"/"control"/"blue"/"green").

### Time range parametresi (default 24h)
`since` query param ile başlangıç zamanı. `OccurredAt >= since` filter. Index `(FeatureFlagId, OccurredAt)` zaten Phase 1'de eklendi, bu sorgu için ideal.

Range opsiyonları UI'da: 1h, 24h, 7d, 30d, all. Backend `since` bekliyor; UI absolute zaman hesaplayıp gönderiyor — backend tarafı semantik enum tutmuyor (esneklik).

### Environment filter opsiyonel
`environmentId` query param verildiğinde aggregate o env'e daraltılır. UI default'ta "tüm env'ler" gösteriyor, dropdown ile filtrelenebiliyor. SQL: `(@envId IS NULL OR ProjectEnvironmentId = @envId)`.

### Timeline (saatlik/günlük bucket) MVP'de yok
"Son 24 saatlik time-series grafik" eklemek için ayrı bucket sorgusu gerekiyor — Phase 2'ye atılıyor. MVP sadece "aralık içinde toplam" gösteriyor.

### CSS bar chart, charting library yok
UI'da inline SVG/CSS ile çubuk grafiği. Recharts/D3/Chart.js eklemiyoruz — bundle size ve dependency'den kaçınıyoruz; basit bar chart için gerek yok.

## MVP kısayolları / Phase 2'de revize edilecekler

- **Cache yok**: her açılışta DB'ye vuruluyor; volume yükseldiğinde 1-2sn gecikmeler olabilir.
- **Pre-aggregated rollup yok**: aynı sorgular tekrar tekrar hesaplanıyor; nightly rollup job (`(flagId, variantId, hour) → count`) volume büyüyünce ekleneckatk.
- **Timeline (time-series) yok**: "saatlik trafik" grafiği yok, sadece toplam.
- **Conversion correlation yok**: "blue gören user'ların kaçı çevirdi" sorusu cevaplanmıyor; ConversionEvent tablosu olmadığı için.
- **CSV/Excel export yok**: UI'da gösteriliyor, indirilemez.
- **Real-time refresh yok**: UI manuel "yenile" butonu; SSE/WebSocket ile live update Phase 2.
- **Filter UX sınırlı**: sadece env filter + time range. User segment, region vs. filter yok.
- **Outcome label hardcoded**: UI tarafında "Off"/"boolean on/off" Türkçe metinler; i18n yok.
- **Top users yok**: "En aktif user kim" tarzı drill-down yok; raw user list query'si yok.

## Dosya haritası

**Backend:**
- `Switchly-2.0.WebApi/Features/Tracking/GetFlagExposureStats/GetFlagExposureStatsHandler.cs` — handler + DTO'lar; GROUP BY (VariantId, IsOn), cross-variant unique count ayrı sorgu
- `Switchly-2.0.WebApi/Features/Tracking/GetFlagExposureStats/GetFlagExposureStatsEndpoint.cs` — `GET /api/flag/{flagId}/exposure-stats`, JWT-protected

**UI (switchly-2.0-ui repo):**
- `lib/types/feature-flag.ts` — `FlagExposureStatsDto`, `VariantExposureStatsDto`, `FlagExposureStatsResponse`
- `lib/services/feature-flag.service.ts` — `getExposureStats(flagId, sinceIsoUtc, environmentId?)`
- `app/dashboard/flags/page.tsx`:
  - `AnalyticsModal` interface + `CLOSED_ANALYTICS_MODAL` sabit + `analyticsModal` state
  - `rangeToSinceIso`, `rangeLabel`, `outcomeLabel` helpers
  - `openAnalyticsModal` + `fetchAnalytics` fonksiyonları
  - "Analytics" butonu (📊 ikonu, mavi, "Ayrıntılar" yanında)
  - Modal JSX: range selector (1h/24h/7d/30d) + summary (total exposures, unique users) + variant breakdown (label + bar + count + %)
