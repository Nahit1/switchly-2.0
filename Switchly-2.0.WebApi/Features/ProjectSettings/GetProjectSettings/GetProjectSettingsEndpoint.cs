using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Switchly_2._0.WebApi.Features.ProjectSettings.GetProjectSettings;

public sealed class GetProjectSettingsEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/settings", async (
                Guid projectId,
                [FromQuery] Guid organizationId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(new GetProjectSettingsQuery(organizationId, projectId), ct);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("ProjectSettings")
            .WithName("GetProjectSettings")
            .RequireAuthorization();
    }
}