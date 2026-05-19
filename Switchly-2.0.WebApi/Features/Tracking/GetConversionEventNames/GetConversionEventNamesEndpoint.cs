using Carter;
using MediatR;

namespace Switchly_2._0.WebApi.Features.Tracking.GetConversionEventNames;

public class GetConversionEventNamesEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/project/{projectId:guid}/conversion-event-names",
            async (Guid projectId, DateTimeOffset? since, IMediator mediator) =>
            {
                // Default: son 30 gün.
                var sinceValue = since ?? DateTimeOffset.UtcNow.AddDays(-30);
                var cmd = new GetConversionEventNamesQuery(projectId, sinceValue);
                var res = await mediator.Send(cmd);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Tracking")
            .WithName("GetConversionEventNames")
            .RequireAuthorization();
    }
}
