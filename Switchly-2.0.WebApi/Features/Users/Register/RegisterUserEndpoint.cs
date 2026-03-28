using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Switchly_2._0.WebApi.Features.Users.Login;

namespace Switchly_2._0.WebApi.Features.Users.Register;

public class RegisterUserEndpoint:ICarterModule
{
    public sealed class Request
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string Name { get; set; } = default!;
    }
    
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/register", async ([FromBody] Request r, IMediator mediator) =>
            {
                // MVP: Password -> “hash” gibi saklandı; gerçek projede hashing ekle
                var cmd = new RegisterUserCommand(r.Email, r.Password, r.Name);
                var res = await mediator.Send(cmd);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Users")
            .WithName("RegisterUser");
    }
}