using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Switchly_2._0.WebApi.Entities;

namespace Switchly_2._0.WebApi.Context.Configurations;

public class FeatureFlagSegmentTargetingVariantWeightConfiguration
    : IEntityTypeConfiguration<FeatureFlagSegmentTargetingVariantWeight>
{
    public void Configure(EntityTypeBuilder<FeatureFlagSegmentTargetingVariantWeight> builder)
    {
        builder.HasKey(x => x.Id);

        // Bir targeting içinde aynı variant iki kez tanımlanmasın
        builder.HasIndex(x => new { x.FeatureFlagSegmentTargetingId, x.VariantId })
            .IsUnique();

        builder.HasOne(x => x.FeatureFlagSegmentTargeting)
            .WithMany(t => t.VariantWeights)
            .HasForeignKey(x => x.FeatureFlagSegmentTargetingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Variant)
            .WithMany(v => v.TargetingWeights)
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
