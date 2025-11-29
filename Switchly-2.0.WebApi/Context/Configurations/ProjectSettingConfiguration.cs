using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Switchly_2._0.WebApi.Entities;

namespace Switchly_2._0.WebApi.Context.Configurations;

public class ProjectSettingConfiguration:IEntityTypeConfiguration<ProjectSetting>
{
    public void Configure(EntityTypeBuilder<ProjectSetting> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(200);

        // Aynı project içinde aynı key 1 kere
        builder.HasIndex(x => new { x.ProjectId, x.Key })
            .IsUnique();

        builder.Property(x => x.DataType)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasOne(x => x.Project)
            .WithMany(p => p.Settings)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}