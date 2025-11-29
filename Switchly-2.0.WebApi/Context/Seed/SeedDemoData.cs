using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Context.Seed;

public static class SeedDemoData
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        var now = DateTimeOffset.UtcNow;

        // USER
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = SeedConstants.UserId,
            Email = "demo@switchly.dev",
            Name = "Demo User",
            PasswordHash = "DEMO_HASH",
            CreatedAt = now
        });

        // ORGANIZATION
        modelBuilder.Entity<Organization>().HasData(new Organization
        {
            Id = SeedConstants.OrganizationId,
            Name = "Demo Organization",
            Slug = "demo-org",
            PublicKey = "demo-org-public",
            SecretKey = "demo-org-secret",
            CreatedAt = now
        });

        // ORGANIZATION MEMBER
        modelBuilder.Entity<OrganizationMember>().HasData(new OrganizationMember
        {
            Id = SeedConstants.OrganizationMemberId,
            OrganizationId = SeedConstants.OrganizationId,
            UserId = SeedConstants.UserId,
            Role = OrganizationRole.Owner,
            CreatedAt = now
        });

        // PROJECT
        modelBuilder.Entity<Project>().HasData(new Project
        {
            Id = SeedConstants.ProjectId,
            OrganizationId = SeedConstants.OrganizationId,
            Name = "Demo Project",
            Key = "demo-project",
            Description = "",
            IsArchived = false,
            CreatedAt = now
        });

        // ENVIRONMENTS
        modelBuilder.Entity<ProjectEnvironment>().HasData(
            new ProjectEnvironment
            {
                Id = SeedConstants.EnvDevId,
                ProjectId = SeedConstants.ProjectId,
                Name = "Development",
                Key = "dev",
                IsDefault = true,
                SortOrder = 1,
                CreatedAt = now
            },
            new ProjectEnvironment
            {
                Id = SeedConstants.EnvStgId,
                ProjectId = SeedConstants.ProjectId,
                Name = "Staging",
                Key = "stg",
                IsDefault = false,
                SortOrder = 2,
                CreatedAt = now
            },
            new ProjectEnvironment
            {
                Id = SeedConstants.EnvProdId,
                ProjectId = SeedConstants.ProjectId,
                Name = "Production",
                Key = "prod",
                IsDefault = false,
                SortOrder = 3,
                CreatedAt = now
            }
        );

        // FEATURE FLAG (boolean)
        modelBuilder.Entity<FeatureFlag>().HasData(new FeatureFlag
        {
            Id = SeedConstants.FeatureFlagId,
            ProjectId = SeedConstants.ProjectId,
            Key = "new_dashboard",
            Name = "New Dashboard",
            Description = "Enable new dashboard",
            Type = FeatureFlagType.Boolean,
            IsArchived = false,
            CreatedAt = now
        });

        // FEATURE FLAG ENVIRONMENTS
        modelBuilder.Entity<FeatureFlagEnvironment>().HasData(
            new FeatureFlagEnvironment
            {
                Id = SeedConstants.FFEnvDevId,
                FeatureFlagId = SeedConstants.FeatureFlagId,
                ProjectEnvironmentId = SeedConstants.EnvDevId,
                IsEnabled = true,
                DefaultRolloutKind = RolloutKind.AllUsers,
                DefaultRolloutPercentage = 100,
                UpdatedAt = now
            },
            new FeatureFlagEnvironment
            {
                Id = SeedConstants.FFEnvStgId,
                FeatureFlagId = SeedConstants.FeatureFlagId,
                ProjectEnvironmentId = SeedConstants.EnvStgId,
                IsEnabled = false,
                DefaultRolloutKind = RolloutKind.Off,
                DefaultRolloutPercentage = 0,
                UpdatedAt = now
            },
            new FeatureFlagEnvironment
            {
                Id = SeedConstants.FFEnvProdId,
                FeatureFlagId = SeedConstants.FeatureFlagId,
                ProjectEnvironmentId = SeedConstants.EnvProdId,
                IsEnabled = false,
                DefaultRolloutKind = RolloutKind.Off,
                DefaultRolloutPercentage = 0,
                UpdatedAt = now
            }
        );
    }
}