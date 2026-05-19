using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Switchly_2._0.WebApi.Entities;

namespace Switchly_2._0.WebApi.Context.Configurations;

public class FlagErrorEventConfiguration : IEntityTypeConfiguration<FlagErrorEvent>
{
    public void Configure(EntityTypeBuilder<FlagErrorEvent> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Severity)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(x => x.Message)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(x => x.Source)
            .HasMaxLength(64);

        // Guardrail birincil access pattern: "flag X için son N dakikadaki error'lar".
        builder.HasIndex(x => new { x.FeatureFlagId, x.OccurredAt });

        // İkincil: env scope'lu sorgular için.
        builder.HasIndex(x => new { x.ProjectEnvironmentId, x.FeatureFlagId, x.OccurredAt });

        builder.HasOne(x => x.Organization)
            .WithMany().HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Project)
            .WithMany().HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ProjectEnvironment)
            .WithMany().HasForeignKey(x => x.ProjectEnvironmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.FeatureFlag)
            .WithMany().HasForeignKey(x => x.FeatureFlagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
