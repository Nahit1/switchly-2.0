using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Switchly_2._0.WebApi.Features.Tracking.TrackErrors;

public class TrackErrorsEndpoint : CarterModule
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
        public string FlagKey { get; set; } = default!;
        public string Severity { get; set; } = "Error";
        public string Message { get; set; } = default!;
        public string? Source { get; set; }
        public string? PropertiesJson { get; set; }
        public DateTimeOffset OccurredAt { get; set; }
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/track/errors", async ([FromBody] Request r, IMediator mediator) =>
            {
                var events = r.Events
                    .Select(e => new ErrorEventInput(
                        e.FlagKey, e.Severity, e.Message, e.Source, e.PropertiesJson, e.OccurredAt))
                    .ToList();
                var cmd = new TrackErrorsCommand(r.PublicKey, r.ProjectKey, r.EnvironmentKey, events);
                var res = await mediator.Send(cmd);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Tracking")
            .WithName("TrackErrors")
            .RequireRateLimiting("track");
    }
}
