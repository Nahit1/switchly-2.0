using Carter;
using MediatR;

namespace Switchly_2._0.WebApi.Features.Flags.GetRuleset;

public sealed class GetRulesetEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/flag/ruleset", async (
                string publicKey,
                string projectKey,
                string environmentKey,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetRulesetQuery(publicKey, projectKey, environmentKey);
                var res = await mediator.Send(query, ct);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Flag")
            .WithName("GetFlagRuleset");
    }
}
