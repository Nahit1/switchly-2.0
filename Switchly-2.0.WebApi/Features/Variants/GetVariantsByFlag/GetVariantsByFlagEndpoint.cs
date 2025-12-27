using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Switchly_2._0.WebApi.Features.Variants.CreateVariant;

namespace Switchly_2._0.WebApi.Features.Variants.GetVariantsByFlag;

public class GetVariantsByFlagEndpoint:CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/flags/{flagId:guid}/variants", async (
                Guid flagId,
                [FromQuery] Guid organizationId,
                [FromQuery] Guid projectId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(new GetVariantsByFlagQuery(organizationId, projectId, flagId), ct);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Variants")
            .WithName("GetVariantsByFlag")
            .RequireAuthorization();
    }
}