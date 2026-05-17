using Carter;
using MediatR;

namespace Switchly_2._0.WebApi.Features.Tracking.GetFlagConversionStats;

public class GetFlagConversionStatsEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/flag/{flagId:guid}/conversion-stats",
            async (Guid flagId,
                   string eventName,
                   DateTimeOffset? since,
                   Guid? environmentId,
                   IMediator mediator) =>
            {
                var sinceValue = since ?? DateTimeOffset.UtcNow.AddDays(-1);
                var cmd = new GetFlagConversionStatsQuery(flagId, eventName, environmentId, sinceValue);
                var res = await mediator.Send(cmd);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Tracking")
            .WithName("GetFlagConversionStats")
            .RequireAuthorization();
    }
}
