using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Switchly_2._0.WebApi.Entities;

namespace Switchly_2._0.WebApi.Context.Configurations;

public class SegmentGroupConfiguration:IEntityTypeConfiguration<SegmentGroup>
{
    public void Configure(EntityTypeBuilder<SegmentGroup> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => new { x.OrganizationId, x.Key })
            .IsUnique();

        builder.Property(x => x.LogicalOperator)
            .HasDefaultValue(Models.Enums.LogicalOperator.And);

        builder.HasOne(x => x.Organization)
            .WithMany(o => o.SegmentGroups)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}