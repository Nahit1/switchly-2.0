using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Switchly_2._0.WebApi.Entities;

namespace Switchly_2._0.WebApi.Context.Configurations;

public class SegmentRuleConfiguration:IEntityTypeConfiguration<SegmentRule>
{
    public void Configure(EntityTypeBuilder<SegmentRule> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.SegmentGroup)
            .WithMany(g => g.Rules)
            .HasForeignKey(x => x.SegmentGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ParentRule)
            .WithMany(p => p.Children)
            .HasForeignKey(x => x.ParentRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.NodeType)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(x => x.LogicalOperator)
            .HasConversion<string?>()
            .HasMaxLength(16);

        builder.Property(x => x.ValueType)
            .HasConversion<string?>()
            .HasMaxLength(16);

        builder.Property(x => x.TraitKey)
            .HasMaxLength(100);

        builder.Property(x => x.Operator)
            .HasMaxLength(32);
    }
}