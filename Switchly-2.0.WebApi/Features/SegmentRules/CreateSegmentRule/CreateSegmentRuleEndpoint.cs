using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.SegmentRules.CreateSegmentRule;

public sealed class CreateSegmentRuleEndpoint : CarterModule
{
    public sealed record CreateSegmentRuleBody(
        string TraitKey,
        string Operator,
        string? Value,
        SegmentValueType ValueType,
        int SortOrder
    );

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/api/segments/{segmentGroupId:guid}/rules",
                async (
                    Guid segmentGroupId,
                    [FromBody] CreateSegmentRuleBody body,
                    IMediator mediator,
                    CancellationToken ct) =>
                {
                    var cmd = new CreateSegmentRuleCommand(
                        segmentGroupId,
                        body.TraitKey,
                        body.Operator,
                        body.Value,
                        body.ValueType,
                        body.SortOrder
                    );

                    var res = await mediator.Send(cmd, ct);
                    return res.Success ? Results.Ok(res) : Results.BadRequest(res);
                })
            .WithTags("Segments")
            .WithName("CreateSegmentRule")
            .RequireAuthorization();
    }
}