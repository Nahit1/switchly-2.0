namespace Switchly_2._0.WebApi.Models.Enums;

public enum SegmentOperator
{
    // Equality
    Equals = 1,
    NotEquals = 2,

    // String / Collection
    Contains = 10,
    StartsWith = 11,
    EndsWith = 12,
    In = 13
}