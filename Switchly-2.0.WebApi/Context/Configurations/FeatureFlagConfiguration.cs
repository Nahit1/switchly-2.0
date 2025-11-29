using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Switchly_2._0.WebApi.Entities;

namespace Switchly_2._0.WebApi.Context.Configurations;

public class FeatureFlagConfiguration:IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        // Project içi unique key
        builder.HasIndex(x => new { x.ProjectId, x.Key })
            .IsUnique();

        // Type (Boolean/Multivariant/Config) enum → string
        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasOne(x => x.Project)
            .WithMany(p => p.FeatureFlags)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}