using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Switchly_2._0.WebApi.Features.ProjectEnviroments.SetTargetingVariantWeights;

public class SetTargetingVariantWeightsEndpoint : CarterModule
{
    public sealed class Request
    {
        public List<WeightItem> Weights { get; set; } = new();
    }

    public sealed class WeightItem
    {
        public Guid VariantId { get; set; }
        public int Weight { get; set; }
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/flag/targeting/{targetingId:guid}/variant-weights",
                async (Guid targetingId, [FromBody] Request r, IMediator mediator) =>
                {
                    var weights = r.Weights
                        .Select(w => new TargetingVariantWeightInput(w.VariantId, w.Weight))
                        .ToList();

                    var cmd = new SetTargetingVariantWeightsCommand(targetingId, weights);
                    var res = await mediator.Send(cmd);
                    return res.Success ? Results.Ok(res) : Results.BadRequest(res);
                })
            .WithTags("Flag")
            .WithName("SetTargetingVariantWeights")
            .RequireAuthorization();
    }
}
