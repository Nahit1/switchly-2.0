using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Switchly_2._0.WebApi.Features.ProjectSettingValues.CreateProjectSettingsValue;

public sealed class CreateProjectSettingsValueEndpoint : CarterModule
{
    public sealed record CreateProjectSettingValueBody(
        Guid OrganizationId,
        string? Value
    );

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/api/projects/{projectId:guid}/settings/{settingId:guid}/values/{environmentId:guid}",
                async (
                    Guid projectId,
                    Guid settingId,
                    Guid environmentId,
                    [FromBody] CreateProjectSettingValueBody body,
                    IMediator mediator,
                    CancellationToken ct) =>
                {
                    var cmd = new CreateProjectSettingValueCommand(
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
            .WithName("CreateProjectSettingValue")
            .RequireAuthorization();
    }
}