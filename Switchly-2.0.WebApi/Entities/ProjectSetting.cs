using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Entities;

public class ProjectSetting
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string Key { get; set; } = default!;   // Checkout:MaxRetries gibi
    public string? Description { get; set; }

    public ProjectSettingDataType DataType { get; set; }
    public bool IsSecret { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    
    public ICollection<ProjectSettingValue> Values { get; set; } = new List<ProjectSettingValue>();
}