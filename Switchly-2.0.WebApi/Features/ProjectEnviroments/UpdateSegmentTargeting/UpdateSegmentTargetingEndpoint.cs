using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.ProjectEnviroments.UpdateSegmentTargeting;

public sealed class UpdateSegmentTargetingEndpoint : CarterModule
{
    public sealed record UpdateSegmentTargetingBody(
        RolloutKind RolloutKind,
        int RolloutPercentage,
        int Priority,
        bool IsEnabled
    );

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/flag-environments/segments/{targetingId:guid}",
                async (
                    Guid targetingId,
                    [FromBody] UpdateSegmentTargetingBody body,
                    IMediator mediator,
                    CancellationToken ct) =>
                {
                    var cmd = new UpdateSegmentTargetingCommand(
                        targetingId,
                        body.RolloutKind,
                        body.RolloutPercentage,
                        body.Priority,
                        body.IsEnabled
                    );

                    var res = await mediator.Send(cmd, ct);
                    return res.Success ? Results.Ok(res) : Results.BadRequest(res);
                })
            .WithTags("FeatureFlags")
            .WithName("UpdateSegmentTargeting")
            .RequireAuthorization();
    }
}
