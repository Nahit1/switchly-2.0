namespace Switchly_2._0.WebApi.Entities;

// Bir kullanıcının bir flag'in bir variant'ına (ya da boolean'da on/off durumuna)
// ilk kez maruz kaldığı an. SDK tarafında (userKey, flagKey, outcome) için
// de-dup edilip batch olarak yazılır.
public class FlagExposureEvent
{
    public Guid Id { get; set; }

    // Hiyerarşiyi denormalize ediyoruz: analytics sorgularında "şu org'un son 24 saatteki
    // tüm exposure'ları" gibi join'siz tarama için. FlagId tek başına yeterli olurdu ama
    // her sorguda flag→project→org join'i pahalı.
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public Guid ProjectEnvironmentId { get; set; }
    public ProjectEnvironment ProjectEnvironment { get; set; } = default!;

    public Guid FeatureFlagId { get; set; }
    public FeatureFlag FeatureFlag { get; set; } = default!;

    // Boolean flag'de null. Multivariant'ta hangi variant'a düştüyse dolu;
    // multivariant'ta hiçbir variant atanmadıysa (Off) yine null + IsOn=false.
    public Guid? VariantId { get; set; }
    public Variant? Variant { get; set; }

    // Boolean'da on/off karşılığı.
    // Multivariant'ta "bir variant atandı mı"; true ise VariantId dolu, false ise null.
    public bool IsOn { get; set; }

    // Consumer'ın SDK'ya verdiği stable user identifier. Bucketing'in de girdisi.
    public string UserKey { get; set; } = default!;

    // SDK'nın evaluate yaptığı an — server'a düştüğü an değil.
    // Queue lag'i olsa bile "user T anında X variant'ını gördü" anlamı kaybolmasın diye.
    public DateTimeOffset OccurredAt { get; set; }
}
