using Carter;
using MediatR;

namespace Switchly_2._0.WebApi.Features.Rollout.ResumeRolloutSchedule;

public class ResumeRolloutScheduleEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/rollout-schedule/{scheduleId:guid}/resume",
            async (Guid scheduleId, IMediator mediator) =>
            {
                var cmd = new ResumeRolloutScheduleCommand(scheduleId);
                var res = await mediator.Send(cmd);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Rollout")
            .WithName("ResumeRolloutSchedule")
            .RequireAuthorization();
    }
}
