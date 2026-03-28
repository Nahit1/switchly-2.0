using Carter;
using MediatR;
using Switchly_2._0.WebApi.Features.Organizations.GetUserOrganizations;

namespace Switchly_2._0.WebApi.Features.Users.CheckUserHasOrganization;

public class CheckUserHasOrganizationEndpoint:ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/user/checkUserHasOrganization", async (IMediator mediator) =>
            {
                
                var res = await mediator.Send(new CheckUserHasOrganizationQuery());
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("User")
            .WithName("CheckUserHasOrganization")
            .RequireAuthorization();
    }
}