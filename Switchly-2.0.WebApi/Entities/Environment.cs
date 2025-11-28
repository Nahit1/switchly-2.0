namespace Switchly_2._0.WebApi.Entities;

public class Environment
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string Name { get; set; } = default!; // Development, Staging, Production
    public string Key { get; set; } = default!;  // dev, stg, prod
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    
    public ICollection<FeatureFlagEnvironment> FeatureFlagEnvironments { get; set; } = new List<FeatureFlagEnvironment>();
    public ICollection<ProjectSettingValue> SettingValues { get; set; } = new List<ProjectSettingValue>();
}