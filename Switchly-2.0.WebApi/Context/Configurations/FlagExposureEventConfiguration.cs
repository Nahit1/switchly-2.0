using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Switchly_2._0.WebApi.Entities;

namespace Switchly_2._0.WebApi.Context.Configurations;

public class FlagExposureEventConfiguration : IEntityTypeConfiguration<FlagExposureEvent>
{
    public void Configure(EntityTypeBuilder<FlagExposureEvent> builder)
    {
        builder.HasKey(x => x.Id);

        // UserKey serbest string ama defansif limit — kötü niyetli ya da hatalı client'ın
        // sonsuz uzunlukta key göndermesini engelle.
        builder.Property(x => x.UserKey)
            .HasMaxLength(256)
            .IsRequired();

        // Birincil analytics access: "şu flag'in son N saatteki event'leri".
        builder.HasIndex(x => new { x.FeatureFlagId, x.OccurredAt });

        // Audit/debug access: "şu user'ın şu flag'deki maruziyet geçmişi".
        builder.HasIndex(x => new { x.UserKey, x.FeatureFlagId });

        // Hiyerarşi FK'leri — parent silinince event'ler de düşer (lifecycle clean).
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

        builder.HasOne(x => x.FeatureFlag)
            .WithMany()
            .HasForeignKey(x => x.FeatureFlagId)
            .OnDelete(DeleteBehavior.Cascade);

        // Variant silinse bile exposure tarihçesi yaşamalı: "user_42 dün blue gördü"
        // bilgisi blue silinince anlamsızlaşmıyor, sadece variant pointer'ı kayboluyor.
        builder.HasOne(x => x.Variant)
            .WithMany()
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
