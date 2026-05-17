using Carter;
using MediatR;

namespace Switchly_2._0.WebApi.Features.SegmentRules.RemoveSegmentRule;

public sealed class RemoveSegmentRuleEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/segments/rules/{ruleId:guid}",
                async (
                    Guid ruleId,
                    IMediator mediator,
                    CancellationToken ct) =>
                {
                    var cmd = new RemoveSegmentRuleCommand(ruleId);
                    var res = await mediator.Send(cmd, ct);
                    return res.Success ? Results.Ok(res) : Results.BadRequest(res);
                })
            .WithTags("Segments")
            .WithName("RemoveSegmentRule")
            .RequireAuthorization();
    }
}
