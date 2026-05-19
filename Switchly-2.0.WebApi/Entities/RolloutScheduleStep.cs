namespace Switchly_2._0.WebApi.Entities;

public class RolloutScheduleStep
{
    public Guid Id { get; set; }

    public Guid RolloutScheduleId { get; set; }
    public RolloutSchedule RolloutSchedule { get; set; } = default!;

    public int StepIndex { get; set; }       // 0, 1, 2, ...

    // Bu adımın hedef yüzdesi.
    // Boolean: DefaultRolloutPercentage olarak uygulanır (RolloutKind = Percentage).
    // Multivariant: TargetVariant'ın weight'i; kalan diğer variant'lara orantılı dağıtılır.
    public int Percentage { get; set; }

    // Bu adımda kalma süresi. Son adımda ignore edilir.
    public int DurationMinutes { get; set; }

    // Promoter bu adıma geçildiğinde damgalar. İlk adım create anında set edilir.
    public DateTimeOffset? PromotedAt { get; set; }
}
