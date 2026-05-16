using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Switchly_2._0.WebApi.Entities;

namespace Switchly_2._0.WebApi.Context.Configurations;

public class FeatureFlagEnvironmentVariantWeightConfiguration
    : IEntityTypeConfiguration<FeatureFlagEnvironmentVariantWeight>
{
    public void Configure(EntityTypeBuilder<FeatureFlagEnvironmentVariantWeight> builder)
    {
        builder.HasKey(x => x.Id);

        // Bir env içinde aynı variant iki kez tanımlanmasın
        builder.HasIndex(x => new { x.FeatureFlagEnvironmentId, x.VariantId })
            .IsUnique();

        builder.HasOne(x => x.FeatureFlagEnvironment)
            .WithMany(fe => fe.VariantWeights)
            .HasForeignKey(x => x.FeatureFlagEnvironmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Variant)
            .WithMany(v => v.EnvironmentWeights)
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
