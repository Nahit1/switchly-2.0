using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Switchly_2._0.WebApi.Features.ProjectSettingValues.GetProjectSettingValueBySetting;

public sealed class GetProjectSettingValueBySettingEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/api/projects/{projectId:guid}/settings/{settingId:guid}/values",
                async (
                    Guid projectId,
                    Guid settingId,
                    [FromQuery] Guid organizationId,
                    IMediator mediator,
                    CancellationToken ct) =>
                {
                    var query = new GetProjectSettingValueBySettingQuery(
                        organizationId,
                        projectId,
                        settingId
                    );

                    var res = await mediator.Send(query, ct);
                    return res.Success ? Results.Ok(res) : Results.BadRequest(res);
                })
            .WithTags("ProjectSettings")
            .WithName("GetProjectSettingValuesBySetting")
            .RequireAuthorization();
    }
}