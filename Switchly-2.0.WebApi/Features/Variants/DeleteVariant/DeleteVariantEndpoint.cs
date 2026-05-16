using Carter;
using MediatR;

namespace Switchly_2._0.WebApi.Features.Variants.DeleteVariant;

public class DeleteVariantEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/variant/{variantId:guid}",
                async (Guid variantId, IMediator mediator) =>
                {
                    var cmd = new DeleteVariantCommand(variantId);
                    var res = await mediator.Send(cmd);
                    return res.Success ? Results.Ok(res) : Results.BadRequest(res);
                })
            .WithTags("Variant")
            .WithName("DeleteVariant")
            .RequireAuthorization();
    }
}
