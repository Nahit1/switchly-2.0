using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Entities;

public class SegmentRule
{
    public Guid Id { get; set; }
    public Guid SegmentGroupId { get; set; }
    public SegmentGroup SegmentGroup { get; set; } = default!;

    public Guid? ParentRuleId { get; set; }
    public SegmentRule? ParentRule { get; set; }
    public ICollection<SegmentRule> Children { get; set; } = new List<SegmentRule>();

    public SegmentNodeType NodeType { get; set; }          // Group, Condition
    public LogicalOperator? LogicalOperator { get; set; }  // Group node için

    // Condition node için:
    public string? TraitKey { get; set; }       // country, plan, version...
    public string? Operator { get; set; }       // Equals, NotEquals, In, Contains...
    public string? Value { get; set; }
    public SegmentValueType? ValueType { get; set; }

    public int SortOrder { get; set; }
}