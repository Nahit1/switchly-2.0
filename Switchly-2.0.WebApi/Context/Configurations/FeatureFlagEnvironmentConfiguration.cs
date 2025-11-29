using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Switchly_2._0.WebApi.Entities;

namespace Switchly_2._0.WebApi.Context.Configurations;

public class FeatureFlagEnvironmentConfiguration:IEntityTypeConfiguration<FeatureFlagEnvironment>
{
    public void Configure(EntityTypeBuilder<FeatureFlagEnvironment> builder)
    {
        builder.HasIndex(x => new { x.FeatureFlagId, x.ProjectEnvironmentId })
            .IsUnique();

        builder.Property(x => x.DefaultRolloutKind)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasOne(x => x.FeatureFlag)
            .WithMany(f => f.Environments)
            .HasForeignKey(x => x.FeatureFlagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ProjectEnvironment)
            .WithMany(e => e.FeatureFlagEnvironments)
            .HasForeignKey(x => x.ProjectEnvironmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}