using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Entities;

// Bir FeatureFlagEnvironment için kademeli rollout planı. Tek aktif schedule per env.
// Adımlar (Steps) zaman bazlı; promoter service süre dolunca otomatik sonraki adıma geçer.
public class RolloutSchedule
{
    public Guid Id { get; set; }

    public Guid FeatureFlagEnvironmentId { get; set; }
    public FeatureFlagEnvironment FeatureFlagEnvironment { get; set; } = default!;

    // Multivariant'ta promote edilen variant; boolean flag'de null.
    public Guid? TargetVariantId { get; set; }
    public Variant? TargetVariant { get; set; }

    public RolloutScheduleStatus Status { get; set; }

    // 0-based index; CurrentStepIndex = 0 ilk adımda demek. Bu adımın weight'leri uygulanmış.
    public int CurrentStepIndex { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    // Şu anki adıma ne zaman geçildi — promoter "süresi doldu mu" hesabını bundan yapıyor.
    public DateTimeOffset? LastTransitionAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // Pause/Resume zaman damgaları — audit ve debugging için.
    public DateTimeOffset? PausedAt { get; set; }

    // Rollback için pre-schedule snapshot (JSON).
    // Boolean: {"isEnabled":true,"defaultRolloutKind":"AllUsers","defaultRolloutPercentage":100}
    // Multivariant: {"targetVariantId":"...","weights":[{"variantId":"...","weight":50}, ...]}
    public string? PreScheduleSnapshot { get; set; }

    // Auto-rollback guardrail: null/0 = guardrail kapalı. Aksi takdirde son ErrorWindowMinutes
    // içinde MinSeverity ve üstü Error event sayısı bu eşiği aşarsa schedule otomatik rollback olur.
    public int? ErrorThreshold { get; set; }
    public int ErrorWindowMinutes { get; set; } = 10;
    public Models.Enums.ErrorSeverity MinSeverity { get; set; } = Models.Enums.ErrorSeverity.Error;

    // "manual" (kullanıcı butona basarak) ya da "errors-exceeded" (guardrail tetikledi).
    // RolledBack state'inde anlamlı, diğerlerinde null.
    public string? RolledBackReason { get; set; }

    public ICollection<RolloutScheduleStep> Steps { get; set; } = new List<RolloutScheduleStep>();
}
