using Carter;
using MediatR;

namespace Switchly_2._0.WebApi.Features.Rollout.PauseRolloutSchedule;

public class PauseRolloutScheduleEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/rollout-schedule/{scheduleId:guid}/pause",
            async (Guid scheduleId, IMediator mediator) =>
            {
                var cmd = new PauseRolloutScheduleCommand(scheduleId);
                var res = await mediator.Send(cmd);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Rollout")
            .WithName("PauseRolloutSchedule")
            .RequireAuthorization();
    }
}
