using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Switchly_2._0.WebApi.Features.ProjectEnviroments.AssignSegmentRule;

public sealed class AssignSegmentRuleEndpoint : CarterModule
{
    public sealed record AssignSegmentRuleBody(
        Guid SegmentGroupId,
        Guid FeatureFlagEnvironmentId
    );

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/flag-environments/segments",
                async (
                    [FromBody] AssignSegmentRuleBody body,
                    IMediator mediator,
                    CancellationToken ct) =>
                {
                    var cmd = new AssignSegmentRuleCommand(
                        body.FeatureFlagEnvironmentId,
                        body.SegmentGroupId
                    );

                    var res = await mediator.Send(cmd, ct);
                    return res.Success
                        ? Results.Ok(res)
                        : Results.BadRequest(res);
                })
            .WithTags("FeatureFlags")
            .WithName("AssignSegmentRule")
            .RequireAuthorization();
    }
}