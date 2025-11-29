using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Switchly_2._0.WebApi.Entities;

namespace Switchly_2._0.WebApi.Context.Configurations;

public class OrganizationConfiguration:IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(x => x.Slug)
            .IsUnique();

        builder.Property(x => x.PublicKey)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(x => x.PublicKey).IsUnique();

        builder.Property(x => x.SecretKey)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasMany(x => x.Projects)
            .WithOne(p => p.Organization)
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.SegmentGroups)
            .WithOne(sg => sg.Organization)
            .HasForeignKey(sg => sg.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}