using Carter;
using MediatR;

namespace Switchly_2._0.WebApi.Features.SegmentRules.GetSegmentRulesByGroup;

public sealed class GetSegmentRulesByGroupEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/api/segments/{segmentGroupId:guid}/rules",
                async (
                    Guid segmentGroupId,
                    IMediator mediator,
                    CancellationToken ct) =>
                {
                    var res = await mediator.Send(new GetSegmentRulesByGroupQuery(segmentGroupId), ct);
                    return res.Success ? Results.Ok(res) : Results.BadRequest(res);
                })
            .WithTags("Segments")
            .WithName("GetSegmentRulesByGroup")
            .RequireAuthorization();
    }
}