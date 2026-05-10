using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.ProjectEnviroments.AssignSegmentRule;

public sealed class AssignSegmentRuleEndpoint : CarterModule
{
    public sealed record AssignSegmentRuleBody(
        Guid SegmentGroupId,
        Guid FeatureFlagEnvironmentId,
        RolloutKind? RolloutKind,
        int? RolloutPercentage,
        int? Priority
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
                        body.SegmentGroupId,
                        body.RolloutKind ?? Models.Enums.RolloutKind.AllUsers,
                        body.RolloutPercentage ?? 100,
                        body.Priority ?? 0
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
