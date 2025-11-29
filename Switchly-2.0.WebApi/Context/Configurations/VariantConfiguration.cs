using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Switchly_2._0.WebApi.Entities;

namespace Switchly_2._0.WebApi.Context.Configurations;

public class VariantConfiguration:IEntityTypeConfiguration<Variant>
{
    public void Configure(EntityTypeBuilder<Variant> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Name)
            .HasMaxLength(255);

        // Aynı flag içinde variant key unique olsun
        builder.HasIndex(x => new { x.FeatureFlagId, x.Key })
            .IsUnique();

        builder.HasOne(x => x.FeatureFlag)
            .WithMany(f => f.Variants)
            .HasForeignKey(x => x.FeatureFlagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}