using Carter;
using MediatR;

namespace Switchly_2._0.WebApi.Features.Rollout.RollbackRolloutSchedule;

public class RollbackRolloutScheduleEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/rollout-schedule/{scheduleId:guid}/rollback",
            async (Guid scheduleId, IMediator mediator) =>
            {
                var cmd = new RollbackRolloutScheduleCommand(scheduleId);
                var res = await mediator.Send(cmd);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Rollout")
            .WithName("RollbackRolloutSchedule")
            .RequireAuthorization();
    }
}
