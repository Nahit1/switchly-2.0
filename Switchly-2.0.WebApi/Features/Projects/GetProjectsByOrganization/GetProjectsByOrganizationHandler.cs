using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
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
    public DateTimeOffset CreatedAt { get; set; }
}

public class GetProjectsByOrganizationHandler(SwitchlyDbContext db)
    :IRequestHandler<GetOrganizationListQuery, Response<List<GetProjectsByOrganizationDto>>>
{
    public async Task<Response<List<GetProjectsByOrganizationDto>>> Handle(GetOrganizationListQuery request, CancellationToken cancellationToken)
    {
        var projects = await db.Projects
            .Where(x=>x.OrganizationId == request.OrganizationId && !x.IsArchived)
            .Include(x=>x.Organization)
            .ToListAsync(cancellationToken: cancellationToken);

        if (projects.Count == 0)
        {
            return Response<List<GetProjectsByOrganizationDto>>.Fail("No projects found");
        }
        var projectList = new List<GetProjectsByOrganizationDto>();
        foreach (var project in projects)
        {
            var item = new GetProjectsByOrganizationDto
            {
                Id = project.Id,
                Name = project.Name,
                Key = project.Key,
                Description = project.Description,
                CreatedAt = project.CreatedAt,
                OrganizationName = project.Organization.Name,
            };
            
            projectList.Add(item);

        }
        
        return Response<List<GetProjectsByOrganizationDto>>.Ok(projectList, "Project list");
    }
}