using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Switchly_2._0.WebApi.Features.ProjectSettingValues.UpdateProjectSettingValue;

public sealed class UpdateProjectSettingValueEndpoint : CarterModule
{
    public sealed record UpdateProjectSettingValueBody(
        Guid OrganizationId,
        string? Value
    );

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut(
                "/api/projects/{projectId:guid}/settings/{settingId:guid}/values/{environmentId:guid}",
                async (
                    Guid projectId,
                    Guid settingId,
                    Guid environmentId,
                    [FromBody] UpdateProjectSettingValueBody body,
                    IMediator mediator,
                    CancellationToken ct) =>
                {
                    var cmd = new UpdateProjectSettingValueCommand(
                        body.OrganizationId,
                        projectId,
                        settingId,
                        environmentId,
                        body.Value
                    );

                    var res = await mediator.Send(cmd, ct);
                    return res.Success ? Results.Ok(res) : Results.BadRequest(res);
                })
            .WithTags("ProjectSettings")
            .WithName("UpdateProjectSettingValue")
            .RequireAuthorization();
    }
}
