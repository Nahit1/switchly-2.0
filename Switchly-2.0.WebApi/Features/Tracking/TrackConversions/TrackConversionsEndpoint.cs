using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Switchly_2._0.WebApi.Features.Tracking.TrackConversions;

public class TrackConversionsEndpoint : CarterModule
{
    public sealed class Request
    {
        public string PublicKey { get; set; } = default!;
        public string ProjectKey { get; set; } = default!;
        public string EnvironmentKey { get; set; } = default!;
        public List<EventItem> Events { get; set; } = new();
    }

    public sealed class EventItem
    {
        public string UserKey { get; set; } = default!;
        public string EventName { get; set; } = default!;
        public decimal? Value { get; set; }
        public string? PropertiesJson { get; set; }
        public DateTimeOffset OccurredAt { get; set; }
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        // /api/track/exposures ile aynı felsefe: unauth, PublicKey ile org-scope.
        app.MapPost("/api/track/conversions", async ([FromBody] Request r, IMediator mediator) =>
            {
                var events = r.Events
                    .Select(e => new ConversionEventInput(
                        e.UserKey, e.EventName, e.Value, e.PropertiesJson, e.OccurredAt))
                    .ToList();

                var cmd = new TrackConversionsCommand(r.PublicKey, r.ProjectKey, r.EnvironmentKey, events);
                var res = await mediator.Send(cmd);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Tracking")
            .WithName("TrackConversions");
    }
}
