using Carter;
using MediatR;

namespace Switchly_2._0.WebApi.Features.Tracking.GetFlagExposureStats;

public class GetFlagExposureStatsEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/flag/{flagId:guid}/exposure-stats",
            async (Guid flagId, DateTimeOffset? since, Guid? environmentId, IMediator mediator) =>
            {
                // Default: son 24 saat.
                var sinceValue = since ?? DateTimeOffset.UtcNow.AddDays(-1);

                var cmd = new GetFlagExposureStatsQuery(flagId, environmentId, sinceValue);
                var res = await mediator.Send(cmd);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Tracking")
            .WithName("GetFlagExposureStats")
            .RequireAuthorization();
    }
}
