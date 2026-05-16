using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Switchly_2._0.WebApi.Features.Variants.UpdateVariant;

public class UpdateVariantEndpoint : CarterModule
{
    public sealed class Request
    {
        public string? Name { get; set; }
        public string? PayloadJson { get; set; }
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/variant/{variantId:guid}",
                async (Guid variantId, [FromBody] Request r, IMediator mediator) =>
                {
                    var cmd = new UpdateVariantCommand(variantId, r.Name, r.PayloadJson);
                    var res = await mediator.Send(cmd);
                    return res.Success ? Results.Ok(res) : Results.BadRequest(res);
                })
            .WithTags("Variant")
            .WithName("UpdateVariant")
            .RequireAuthorization();
    }
}
