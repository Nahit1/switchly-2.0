using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Switchly_2._0.WebApi.Features.Rollout.CreateRolloutSchedule;

public class CreateRolloutScheduleEndpoint : CarterModule
{
    public sealed class Request
    {
        public Guid? TargetVariantId { get; set; }
        public List<StepItem> Steps { get; set; } = new();
        public int? ErrorThreshold { get; set; }
        public int? ErrorWindowMinutes { get; set; }
        public string? MinSeverity { get; set; }
    }

    public sealed class StepItem
    {
        public int Percentage { get; set; }
        public int DurationMinutes { get; set; }
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/flag-environment/{flagEnvironmentId:guid}/rollout-schedule",
            async (Guid flagEnvironmentId, [FromBody] Request r, IMediator mediator) =>
            {
                var steps = r.Steps
                    .Select(s => new RolloutStepInput(s.Percentage, s.DurationMinutes))
                    .ToList();
                var cmd = new CreateRolloutScheduleCommand(
                    flagEnvironmentId, r.TargetVariantId, steps,
                    r.ErrorThreshold, r.ErrorWindowMinutes, r.MinSeverity);
                var res = await mediator.Send(cmd);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Rollout")
            .WithName("CreateRolloutSchedule")
            .RequireAuthorization();
    }
}
