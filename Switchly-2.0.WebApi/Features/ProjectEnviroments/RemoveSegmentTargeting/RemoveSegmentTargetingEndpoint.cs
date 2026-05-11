using Carter;
using MediatR;

namespace Switchly_2._0.WebApi.Features.ProjectEnviroments.RemoveSegmentTargeting;

public sealed class RemoveSegmentTargetingEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/flag-environments/segments/{targetingId:guid}",
                async (
                    Guid targetingId,
                    IMediator mediator,
                    CancellationToken ct) =>
                {
                    var cmd = new RemoveSegmentTargetingCommand(targetingId);
                    var res = await mediator.Send(cmd, ct);
                    return res.Success ? Results.Ok(res) : Results.BadRequest(res);
                })
            .WithTags("FeatureFlags")
            .WithName("RemoveSegmentTargeting")
            .RequireAuthorization();
    }
}
