using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Switchly_2._0.WebApi.Features.SegmetGroups.GetAllSegmentGroups;

public sealed class GetAllSegmentGroupEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/api/segments",
                async (
                    [FromQuery] Guid organizationId,
                    IMediator mediator,
                    CancellationToken ct) =>
                {
                    var res = await mediator.Send(new GetAllSegmentGroupsQuery(organizationId), ct);
                    return res.Success ? Results.Ok(res) : Results.BadRequest(res);
                })
            .WithTags("Segments")
            .WithName("GetAllSegmentGroups")
            .RequireAuthorization();
    }
}