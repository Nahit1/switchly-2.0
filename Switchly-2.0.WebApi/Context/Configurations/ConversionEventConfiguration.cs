using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Switchly_2._0.WebApi.Entities;

namespace Switchly_2._0.WebApi.Context.Configurations;

public class ConversionEventConfiguration : IEntityTypeConfiguration<ConversionEvent>
{
    public void Configure(EntityTypeBuilder<ConversionEvent> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserKey)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.EventName)
            .HasMaxLength(128)
            .IsRequired();

        // numeric(18,4) — para birimi için 4 ondalık yeterli; daha yüksek precision'da
        // exchange rate vs hesap çıkıyor. Sınırlı tutarak Postgres storage avantajı.
        builder.Property(x => x.Value)
            .HasPrecision(18, 4);

        // Analytics primary access pattern: "şu kullanıcının tüm conversion'ları"
        // (exposure × conversion join'inde dominant filter UserKey).
        builder.HasIndex(x => new { x.UserKey, x.OccurredAt });

        // İkinci pattern: "son 24h checkout_completed event'leri" — env scope'lu.
        builder.HasIndex(x => new { x.ProjectEnvironmentId, x.EventName, x.OccurredAt });

        // Hiyerarşi FK'leri: parent silinince conversion'lar da düşer.
        // Exposure ile aynı strateji.
        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Project)
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ProjectEnvironment)
            .WithMany()
            .HasForeignKey(x => x.ProjectEnvironmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
