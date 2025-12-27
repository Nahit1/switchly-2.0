using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.ProjectSettings.CreateProjectSettings;

public sealed class CreateProjectSettingsEndpoint : CarterModule
{
    public sealed record CreateProjectSettingBody(
        Guid OrganizationId,
        string Key,
        string? Description,
        ProjectSettingDataType DataType,
        bool IsSecret
    );

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId:guid}/settings", async (
                Guid projectId,
                [FromBody] CreateProjectSettingBody body,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var cmd = new CreateProjectSettingCommand(
                    body.OrganizationId,
                    projectId,
                    body.Key,
                    body.Description,
                    body.DataType,
                    body.IsSecret
                );

                var res = await mediator.Send(cmd, ct);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("ProjectSettings")
            .WithName("CreateProjectSetting")
            .RequireAuthorization();
    }
}