using Carter;
using MediatR;

namespace Switchly_2._0.WebApi.Features.Tracking.GetFlagExposureTimeline;

public class GetFlagExposureTimelineEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/flag/{flagId:guid}/exposure-timeline",
            async (Guid flagId,
                   string? bucketSize,
                   DateTimeOffset? since,
                   Guid? environmentId,
                   IMediator mediator) =>
            {
                var sinceValue = since ?? DateTimeOffset.UtcNow.AddDays(-1);
                var parsedBucket = (bucketSize ?? "hour").Trim().ToLowerInvariant() switch
                {
                    "day" => TimelineBucketSize.Day,
                    _ => TimelineBucketSize.Hour
                };

                var cmd = new GetFlagExposureTimelineQuery(flagId, environmentId, sinceValue, parsedBucket);
                var res = await mediator.Send(cmd);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Tracking")
            .WithName("GetFlagExposureTimeline")
            .RequireAuthorization();
    }
}
