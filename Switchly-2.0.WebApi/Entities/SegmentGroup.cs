namespace Switchly_2._0.WebApi.Entities;

public class SegmentGroup
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string Key { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    
    public ICollection<SegmentRule> Rules { get; set; } = new List<SegmentRule>();
    public ICollection<FeatureFlagSegmentTargeting> FeatureFlagTargetings { get; set; } = new List<FeatureFlagSegmentTargeting>();

}