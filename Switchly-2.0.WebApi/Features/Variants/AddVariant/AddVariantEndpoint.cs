using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Switchly_2._0.WebApi.Features.Variants.AddVariant;

public class AddVariantEndpoint : CarterModule
{
    public sealed class Request
    {
        public string Key { get; set; } = default!;
        public string? Name { get; set; }
        public string? PayloadJson { get; set; }
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/flag/{flagId:guid}/variants",
                async (Guid flagId, [FromBody] Request r, IMediator mediator) =>
                {
                    var cmd = new AddVariantCommand(flagId, r.Key, r.Name, r.PayloadJson);
                    var res = await mediator.Send(cmd);
                    return res.Success ? Results.Ok(res) : Results.BadRequest(res);
                })
            .WithTags("Variant")
            .WithName("AddVariant")
            .RequireAuthorization();
    }
}
