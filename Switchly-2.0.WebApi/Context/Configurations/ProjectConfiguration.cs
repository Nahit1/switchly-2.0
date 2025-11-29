using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Switchly_2._0.WebApi.Entities;

namespace Switchly_2._0.WebApi.Context.Configurations;

public class ProjectConfiguration:IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(100);

        // Aynı org içinde Project.Key unique olsun
        builder.HasIndex(x => new { x.OrganizationId, x.Key })
            .IsUnique();

        builder.HasOne(x => x.Organization)
            .WithMany(o => o.Projects)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}