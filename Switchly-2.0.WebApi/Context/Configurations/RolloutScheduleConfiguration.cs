using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Context.Configurations;

public class RolloutScheduleConfiguration : IEntityTypeConfiguration<RolloutSchedule>
{
    public void Configure(EntityTypeBuilder<RolloutSchedule> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(x => x.MinSeverity)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(x => x.RolledBackReason)
            .HasMaxLength(32);

        builder.HasOne(x => x.FeatureFlagEnvironment)
            .WithMany()
            .HasForeignKey(x => x.FeatureFlagEnvironmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TargetVariant)
            .WithMany()
            .HasForeignKey(x => x.TargetVariantId)
            .OnDelete(DeleteBehavior.SetNull);

        // Tek aktif schedule per env: filtered unique index (Postgres).
        // Birden fazla terminal (Completed/RolledBack) schedule olabilir, ama Active+Paused tek olsun.
        builder.HasIndex(x => x.FeatureFlagEnvironmentId)
            .HasFilter($"\"Status\" IN ('{RolloutScheduleStatus.Active}', '{RolloutScheduleStatus.Paused}', '{RolloutScheduleStatus.Draft}')")
            .IsUnique();

        builder.HasMany(x => x.Steps)
            .WithOne(s => s.RolloutSchedule)
            .HasForeignKey(s => s.RolloutScheduleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
