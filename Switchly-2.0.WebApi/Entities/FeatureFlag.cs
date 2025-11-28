using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Entities;

public class FeatureFlag
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string Key { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public FeatureFlagType Type { get; set; }   // Boolean, Multivariant, Config
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    
    public ICollection<Variant> Variants { get; set; } = new List<Variant>();
    public ICollection<FeatureFlagEnvironment> Environments { get; set; } = new List<FeatureFlagEnvironment>();
}