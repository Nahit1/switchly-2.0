using Carter;
using MediatR;

namespace Switchly_2._0.WebApi.Features.Rollout.GetRolloutSchedule;

public class GetRolloutScheduleEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/flag-environment/{flagEnvironmentId:guid}/rollout-schedule",
            async (Guid flagEnvironmentId, IMediator mediator) =>
            {
                var cmd = new GetRolloutScheduleQuery(flagEnvironmentId);
                var res = await mediator.Send(cmd);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Rollout")
            .WithName("GetRolloutSchedule")
            .RequireAuthorization();
    }
}
