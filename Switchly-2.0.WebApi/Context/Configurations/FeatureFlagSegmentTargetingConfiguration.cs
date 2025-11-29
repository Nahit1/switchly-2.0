using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Switchly_2._0.WebApi.Entities;

namespace Switchly_2._0.WebApi.Context.Configurations;

public class FeatureFlagSegmentTargetingConfiguration:IEntityTypeConfiguration<FeatureFlagSegmentTargeting>
{
    public void Configure(EntityTypeBuilder<FeatureFlagSegmentTargeting> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RolloutKind)
            .HasConversion<string>()
            .HasMaxLength(32);

        // Bir env-flag için aynı segment bir kez tanımlansın
        builder.HasIndex(x => new { x.FeatureFlagEnvironmentId, x.SegmentGroupId })
            .IsUnique();

        builder.HasOne(x => x.FeatureFlagEnvironment)
            .WithMany(fe => fe.SegmentTargetings)
            .HasForeignKey(x => x.FeatureFlagEnvironmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SegmentGroup)
            .WithMany(sg => sg.FeatureFlagTargetings)
            .HasForeignKey(x => x.SegmentGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}