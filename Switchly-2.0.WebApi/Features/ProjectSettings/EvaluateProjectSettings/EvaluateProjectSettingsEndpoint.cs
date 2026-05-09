using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Switchly_2._0.WebApi.Features.ProjectSettings.EvaluateProjectSettings;

public sealed class EvaluateProjectSettingsEndpoint : CarterModule
{
    public sealed record EvaluateProjectSettingsBody(
        string PublicKey,
        string ProjectKey,
        string EnvironmentKey
    );

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/settings/evaluate", async (
                [FromBody] EvaluateProjectSettingsBody body,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var cmd = new EvaluateProjectSettingsRequest(
                    body.PublicKey,
                    body.ProjectKey,
                    body.EnvironmentKey
                );

                var res = await mediator.Send(cmd, ct);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("ProjectSettings")
            .WithName("EvaluateProjectSettings");
    }
}
