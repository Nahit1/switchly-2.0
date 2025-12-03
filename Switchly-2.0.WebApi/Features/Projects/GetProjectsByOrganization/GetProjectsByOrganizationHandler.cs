using MediatR;
using Switchly_2._0.WebApi.Models.Common;

namespace Switchly_2._0.WebApi.Features.Projects.GetProjectsByOrganization;

public sealed record GetOrganizationListQuery(Guid OrganizationId)
    : IRequest<Response<List<GetProjectsByOrganizationDto>>>;

public sealed record GetProjectsByOrganizationDto
{
    public Guid Id { get; set; }
    public string OrganizationName { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string Key { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    
    // public ICollection<ProjectEnvironment> Environments { get; set; } = new List<ProjectEnvironment>();
    // public ICollection<FeatureFlag> FeatureFlags { get; set; } = new List<FeatureFlag>();
    // public ICollection<ProjectSetting> Settings { get; set; } = new List<ProjectSetting>();
}

public class GetProjectsByOrganizationHandler
{
    
}