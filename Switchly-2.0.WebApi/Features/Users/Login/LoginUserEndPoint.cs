using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Switchly_2._0.WebApi.Features.Users.Login;

public class LoginUserEndPoint:ICarterModule
{
    public sealed class Request
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
    
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/login", async ([FromBody] Request r, IMediator mediator) =>
            {
                // MVP: Password -> “hash” gibi saklandı; gerçek projede hashing ekle
                var cmd = new LoginUserCommand(r.Email, r.Password);
                var res = await mediator.Send(cmd);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Users")
            .WithName("LoginUser");
    }
}