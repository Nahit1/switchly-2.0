using Carter;
using MediatR;

namespace Switchly_2._0.WebApi.Features.SegmetGroups.RemoveSegmentGroup;

public sealed class RemoveSegmentGroupEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/segments/{segmentGroupId:guid}",
                async (
                    Guid segmentGroupId,
                    IMediator mediator,
                    CancellationToken ct) =>
                {
                    var cmd = new RemoveSegmentGroupCommand(segmentGroupId);
                    var res = await mediator.Send(cmd, ct);
                    return res.Success ? Results.Ok(res) : Results.BadRequest(res);
                })
            .WithTags("Segments")
            .WithName("RemoveSegmentGroup")
            .RequireAuthorization();
    }
}
