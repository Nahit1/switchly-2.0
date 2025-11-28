namespace Switchly_2._0.WebApi.Entities;

public class Project
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string Key { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    
    public ICollection<Environment> Environments { get; set; } = new List<Environment>();
    public ICollection<FeatureFlag> FeatureFlags { get; set; } = new List<FeatureFlag>();
    public ICollection<ProjectSetting> Settings { get; set; } = new List<ProjectSetting>();
}