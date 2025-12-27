using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Switchly_2._0.WebApi.Features.ProjectEnviroments.GetProjectEnviromentsByProject;

public sealed class GetProjectEnviromentsByProjectEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/api/projects/{projectId:guid}/environments",
                async (
                    Guid projectId,
                    [FromQuery] Guid organizationId,
                    IMediator mediator,
                    CancellationToken ct) =>
                {
                    var query = new GetProjectEnviromentsByProjectQuery(
                        organizationId,
                        projectId
                    );

                    var res = await mediator.Send(query, ct);
                    return res.Success ? Results.Ok(res) : Results.BadRequest(res);
                })
            .WithTags("ProjectEnvironments")
            .WithName("GetProjectEnvironmentsByProject")
            .RequireAuthorization();
    }
}