using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Context.Seed;
using Switchly_2._0.WebApi.Entities;
using Environment = System.Environment;

namespace Switchly_2._0.WebApi.Context;

public class SwitchlyDbContext(DbContextOptions<SwitchlyDbContext> opts):DbContext(opts)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectSetting> ProjectSettings => Set<ProjectSetting>();
    public DbSet<ProjectSettingValue> ProjectSettingValues => Set<ProjectSettingValue>();
    public DbSet<ProjectEnvironment> ProjectEnvironments => Set<ProjectEnvironment>();
    public DbSet<Variant> Variants => Set<Variant>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<FeatureFlagEnvironment> FeatureFlagEnvironments => Set<FeatureFlagEnvironment>();
    public DbSet<FeatureFlagSegmentTargeting> FeatureFlagSegmentTargetings => Set<FeatureFlagSegmentTargeting>();
    public DbSet<FeatureFlagEnvironmentVariantWeight> FeatureFlagEnvironmentVariantWeights => Set<FeatureFlagEnvironmentVariantWeight>();
    public DbSet<FeatureFlagSegmentTargetingVariantWeight> FeatureFlagSegmentTargetingVariantWeights => Set<FeatureFlagSegmentTargetingVariantWeight>();
    public DbSet<SegmentGroup> SegmentGroups => Set<SegmentGroup>();
    public DbSet<SegmentRule> SegmentRules => Set<SegmentRule>();
    public DbSet<FlagExposureEvent> FlagExposureEvents => Set<FlagExposureEvent>();
    public DbSet<ConversionEvent> ConversionEvents => Set<ConversionEvent>();
    public DbSet<RolloutSchedule> RolloutSchedules => Set<RolloutSchedule>();
    public DbSet<RolloutScheduleStep> RolloutScheduleSteps => Set<RolloutScheduleStep>();
    public DbSet<FlagErrorEvent> FlagErrorEvents => Set<FlagErrorEvent>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SwitchlyDbContext).Assembly);
        
        modelBuilder.ApplySeed();
    }
}