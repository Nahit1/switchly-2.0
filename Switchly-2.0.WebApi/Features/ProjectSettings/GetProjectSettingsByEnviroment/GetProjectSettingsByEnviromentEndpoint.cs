using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Switchly_2._0.WebApi.Features.ProjectSettings.GetProjectSettings;

namespace Switchly_2._0.WebApi.Features.ProjectSettings.GetProjectSettingsByEnviroment;

public class GetProjectSettingsByEnviromentEndpoint:CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/settingsbyenvironment", async (
                Guid projectId,
                [FromQuery] Guid organizationId,
                [FromQuery] Guid environmentId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(new GetProjectSettingsByEnvironmentQuery(organizationId,environmentId, projectId), ct);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("ProjectSettings")
            .WithName("GetProjectSettingsByEnvironment")
            .RequireAuthorization();
    }
}