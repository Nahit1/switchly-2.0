using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Switchly_2._0.WebApi.Features.Users.Login;

namespace Switchly_2._0.WebApi.Features.Organizations.CreateOrganization;

public class CreateOrganizationEndpoint:ICarterModule
{
    public sealed class Request
    {
        public string Name { get; set; } = default!;
    }
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/organization/create", async ([FromBody] Request r, IMediator mediator) =>
            {
                // MVP: Password -> “hash” gibi saklandı; gerçek projede hashing ekle
                var cmd = new CreateOrganizationCommand(r.Name);
                var res = await mediator.Send(cmd);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Organization")
            .WithName("CreateOrganization")
            .RequireAuthorization();
    }
}