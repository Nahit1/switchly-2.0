using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Switchly_2._0.WebApi.Entities;

namespace Switchly_2._0.WebApi.Context.Configurations;

public class ProjectSettingValueConfiguration:IEntityTypeConfiguration<ProjectSettingValue>
{
    public void Configure(EntityTypeBuilder<ProjectSettingValue> builder)
    {
        builder.HasKey(x => x.Id);

        // Bir setting + env için 1 değer
        builder.HasIndex(x => new { x.ProjectSettingId, x.ProjectEnvironmentId })
            .IsUnique();

        builder.HasOne(x => x.ProjectSetting)
            .WithMany(s => s.Values)
            .HasForeignKey(x => x.ProjectSettingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ProjectEnvironment)
            .WithMany(e => e.SettingValues)
            .HasForeignKey(x => x.ProjectEnvironmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}