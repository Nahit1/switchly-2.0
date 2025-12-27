using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Switchly_2._0.WebApi.Features.Projects.CreateProject;

namespace Switchly_2._0.WebApi.Features.Projects.GetProjectsByOrganization;

public class GetProjectsByOrganizationEndpoint:ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/project/get-projects-by-organization", async ([FromQuery]Guid OrganizationId, IMediator mediator) =>
            {
                var res = await mediator.Send(new GetOrganizationListQuery(OrganizationId));
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Project")
            .WithName("GetProjectsByOrganization")
            .RequireAuthorization();
    }
}