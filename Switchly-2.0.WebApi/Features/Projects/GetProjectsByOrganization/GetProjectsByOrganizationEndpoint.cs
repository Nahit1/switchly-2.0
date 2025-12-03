using MediatR;
using Switchly_2._0.WebApi.Features.Projects.CreateProject;

namespace Switchly_2._0.WebApi.Features.Projects.GetProjectsByOrganization;

public class GetProjectsByOrganizationEndpoint
{
    public sealed class Request
    {
        public Guid OrganizationId { get; set; }
    }
    
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/project/get-projects-by-organization", async (Request r, IMediator mediator) =>
            {
                var res = await mediator.Send(new GetOrganizationListQuery(r.OrganizationId));
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Project")
            .WithName("GetProjectsByOrganization")
            .RequireAuthorization();
    }
}