using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Switchly_2._0.WebApi.Features.Flags.CreateFlag;

namespace Switchly_2._0.WebApi.Features.Flags.EvaluateFlag;

public class EvaluateFlagEndpoint:CarterModule
{
    public sealed class Request
    {
        public string PublicKey { get; set; }
        public string ProjectKey { get; set; }
        public string EnvironmentKey { get; set; }
        public string FlagKey { get; set; }
        public Dictionary<string, string>? Traits  { get; set; }
        
    }
    
    
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/flag/evaluate", async ([FromBody] Request r, IMediator mediator) =>
            {
                // MVP: Password -> “hash” gibi saklandı; gerçek projede hashing ekle
                var cmd = new EvaluateFlagRequest(r.PublicKey, r.ProjectKey, r.EnvironmentKey, r.FlagKey, r.Traits);
                var res = await mediator.Send(cmd);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Flag")
            .WithName("EvaluateFlag");
    }
}