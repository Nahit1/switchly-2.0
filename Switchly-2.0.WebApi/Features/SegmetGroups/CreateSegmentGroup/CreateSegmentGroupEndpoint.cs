using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.SegmetRules.CreateSegmentRules;

public sealed class CreateSegmentGroupEndpoint : CarterModule
{
    public sealed record CreateSegmentGroupBody(
        Guid OrganizationId,
        string Key,
        string Name,
        string? Description,
        LogicalOperator? LogicalOperator
    );

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/api/segments",
                async (
                    [FromBody] CreateSegmentGroupBody body,
                    IMediator mediator,
                    CancellationToken ct) =>
                {
                    var cmd = new CreateSegmentGroupCommand(
                        body.OrganizationId,
                        body.Key,
                        body.Name,
                        body.Description,
                        body.LogicalOperator ?? Models.Enums.LogicalOperator.And
                    );

                    var res = await mediator.Send(cmd, ct);
                    return res.Success ? Results.Ok(res) : Results.BadRequest(res);
                })
            .WithTags("Segments")
            .WithName("CreateSegmentGroup")
            .RequireAuthorization();
    }
}
