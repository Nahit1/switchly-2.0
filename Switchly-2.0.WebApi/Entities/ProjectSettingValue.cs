namespace Switchly_2._0.WebApi.Entities;

public class ProjectSettingValue
{
    public Guid Id { get; set; }

    public Guid ProjectSettingId { get; set; }
    public ProjectSetting ProjectSetting { get; set; } = default!;

    public Guid ProjectEnvironmentId { get; set; }
    public ProjectEnvironment ProjectEnvironment { get; set; } = default!;

    public string Value { get; set; } = default!;
    public DateTimeOffset UpdatedAt { get; set; }
}