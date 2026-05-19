# Progressive Rollout (Seviye 1)

**Status:** Seviye 1 + Seviye 2 complete (zaman bazlı promote + manuel kontrol + error-triggered auto-rollback).
**Started:** 2026-05-18
**Seviye 1 completed:** 2026-05-18
**Seviye 2 completed:** 2026-05-19

## Özet

Bir flag-env'e kademeli rollout planı: "%10 → 4 saat bekle → %25 → 1 gün bekle → %100". Background service her dakika kontrol eder, süresi dolan adımda otomatik bir sonrakine geçer. Kullanıcı **Pause/Resume/Rollback** ile manuel müdahale edebilir.

- **Boolean flag**: her step `DefaultRolloutPercentage`'ı belirtir.
- **Multivariant flag**: target variant seçilir, her step bu variant'ın weight'i. Diğer variant'lar kalan'ı **mevcut oranlarında** paylaşır.

Seviye 2'de bu altyapının üstüne **error tracking API** (`POST /api/track/errors`) + **error-threshold guardrail** eklenecek; aşılırsa otomatik rollback (Rollback butonunun aynısı). Burada tasarlanan rollback davranışı (target=%0, baseline=%100) auto-rollback'in de davranışı olacak.

## Mimari kararlar ve gerekçeleri

### Tek variant promote (multivariant'ta)
Schedule bir target variant'ı promote eder; diğer variant'lar kalan yüzdeyi orantılı paylaşır. Sebep:
- En yaygın use case ("yeni'yi kademeli aç").
- UI basit: tek variant + steps listesi.
- "Her step'te tüm dağılım" alternatifi daha esnek ama UI çok karmaşık, MVP'de değerine değmez.

### Pre-schedule snapshot
Schedule başlamadan önceki state (boolean: enabled+kind+%; multivariant: tüm weight'ler) JSON olarak `RolloutSchedule.PreScheduleSnapshot`'ta saklanır. Rollback bu snapshot'ı geri yükler — "schedule hiç olmamış gibi" semantiği.

### Üç status: Active, Paused, RolledBack, Completed (Draft opsiyonel)
- **Draft**: henüz başlatılmadı (param ayarlanıyor). Şimdilik kullanmıyoruz; create direkt Active başlatır. Yeri açık bırakıyoruz.
- **Active**: auto-promoter çalışıyor.
- **Paused**: zaman geçse de adım atlanmıyor. Manuel "Resume" gerek.
- **Completed**: son step'e ulaşıldı. Terminal.
- **RolledBack**: rollback yapıldı. Terminal.

### Bir flag-env'de tek schedule
Aynı anda 2 schedule olmasın — race condition, weight conflict. Yeni schedule oluşturmak için eskisinin Completed/RolledBack olması gerek.

### Background promoter her dakika çalışır
`PeriodicTimer(1 min)`. Cron yerine basit BackgroundService. Sub-minute precision gerekirse interval düşürülür.

### Promote = weight update + transition timestamp
Bir adım atlandığında: ilgili weight'leri DB'de günceller, `LastTransitionAt = now`, `CurrentStepIndex++`. Bu transaction tek SaveChanges.

### Step duration son adımda kullanılmıyor
Son step'te promote zaten yok (zaten %100); duration alanı ignore. UI'da girilse de promoter ona bakmıyor.

## MVP kısayolları / sonra ele alınacaklar

- **Error guardrail yok** (Seviye 2'ye saklandı).
- **Statistical significance guardrail yok**: "yeni %20 conversion düşerse rollback" gibi metrik-bazlı rule yok. Phase 3.
- **Schedule editing yok**: aktif schedule'ı düzenleyemiyorsun (sadece pause/rollback). Editing isteniyorsa: rollback → yeni schedule oluştur.
- **Bildirim yok**: promote olduğunda email/Slack notification yok. Phase 2.
- **Audit log yok**: kim ne zaman pause/rollback yaptı kayıt yok. Phase 2.
- **Çoklu schedule paralel yok**: bir flag-env'de max 1 schedule. Diğer flag-env'lerde paralel mümkün.
- **Cross-env propagation yok**: dev'de Completed olduktan sonra prod'a otomatik kopyalama yok. Manuel.
- **Sub-minute precision yok**: 1 dakikadan kısa step duration anlamsız.
- **Last step duration**: UI'da girilse de ignore. UX'de "son adımda süre yok" hint ekle.

## Dosya haritası

**Backend (yazılacak):**
- `Switchly-2.0.WebApi/Entities/RolloutSchedule.cs`
- `Switchly-2.0.WebApi/Entities/RolloutScheduleStep.cs`
- `Switchly-2.0.WebApi/Models/Enums/RolloutScheduleStatus.cs`
- `Switchly-2.0.WebApi/Context/Configurations/RolloutSchedule*Configuration.cs`
- `Switchly-2.0.WebApi/Context/Migrations/<ts>_AddRolloutSchedules.cs`
- `Switchly-2.0.WebApi/Features/Rollout/` — CreateSchedule, GetSchedule, PauseSchedule, ResumeSchedule, RollbackSchedule
- `Switchly-2.0.WebApi/Services/RolloutPromoterService.cs` (IHostedService)

**UI (yazılacak):**
- `lib/types/rollout.ts` — types
- `lib/services/rollout.service.ts`
- `app/dashboard/flags/page.tsx` — env section'a schedule kartı + create modal + butonlar
