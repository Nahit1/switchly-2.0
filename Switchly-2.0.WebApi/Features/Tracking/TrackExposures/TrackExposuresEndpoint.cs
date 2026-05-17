using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Switchly_2._0.WebApi.Features.Tracking.TrackExposures;

public class TrackExposuresEndpoint : CarterModule
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
        public string FlagKey { get; set; } = default!;
        public string? VariantKey { get; set; }
        public bool IsOn { get; set; }
        public DateTimeOffset OccurredAt { get; set; }
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        // /api/flag/evaluate gibi: unauthenticated, PublicKey ile org-scope kontrol ediliyor.
        app.MapPost("/api/track/exposures", async ([FromBody] Request r, IMediator mediator) =>
            {
                var events = r.Events
                    .Select(e => new ExposureEventInput(
                        e.UserKey, e.FlagKey, e.VariantKey, e.IsOn, e.OccurredAt))
                    .ToList();

                var cmd = new TrackExposuresCommand(r.PublicKey, r.ProjectKey, r.EnvironmentKey, events);
                var res = await mediator.Send(cmd);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Tracking")
            .WithName("TrackExposures");
    }
}
