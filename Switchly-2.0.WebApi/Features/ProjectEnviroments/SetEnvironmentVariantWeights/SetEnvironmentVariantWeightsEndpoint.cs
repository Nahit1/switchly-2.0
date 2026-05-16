using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Switchly_2._0.WebApi.Features.ProjectEnviroments.SetEnvironmentVariantWeights;

public class SetEnvironmentVariantWeightsEndpoint : CarterModule
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
        app.MapPut("/api/flag/environment/{flagEnvironmentId:guid}/variant-weights",
                async (Guid flagEnvironmentId, [FromBody] Request r, IMediator mediator) =>
                {
                    var weights = r.Weights
                        .Select(w => new VariantWeightInput(w.VariantId, w.Weight))
                        .ToList();

                    var cmd = new SetEnvironmentVariantWeightsCommand(flagEnvironmentId, weights);
                    var res = await mediator.Send(cmd);
                    return res.Success ? Results.Ok(res) : Results.BadRequest(res);
                })
            .WithTags("Flag")
            .WithName("SetEnvironmentVariantWeights")
            .RequireAuthorization();
    }
}
