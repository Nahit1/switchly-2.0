using Switchly_2._0.WebApi.Models.Enums;

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

    /// <summary>
    /// Bu segment'in kuralları nasıl birleştirilir? And = hepsi eşleşmeli, Or = en az biri eşleşmeli.
    /// MVP'de Group/Condition tree desteklenmiyor, bu flat operator hepsi için geçerli.
    /// </summary>
    public LogicalOperator LogicalOperator { get; set; } = LogicalOperator.And;

    public ICollection<SegmentRule> Rules { get; set; } = new List<SegmentRule>();
    public ICollection<FeatureFlagSegmentTargeting> FeatureFlagTargetings { get; set; } = new List<FeatureFlagSegmentTargeting>();

}