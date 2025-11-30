using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Shared;

namespace Switchly_2._0.WebApi.Features.Users.Login;

public sealed record LoginUserCommand(string Email, string Password)
    : IRequest<Response<UserLoginDto>>;


public sealed record UserLoginDto
{
    public Guid UserId { get; init; }
    public ICollection<OrganizationRoleDto> Organizations { get; init; } = new List<OrganizationRoleDto>();
    public string Token { get; init; } = string.Empty;
}


public class RegisterHandlerCommandValidator : AbstractValidator<LoginUserCommand>
{
    public RegisterHandlerCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class LoginUserHandler(SwitchlyDbContext context,  IJwtTokenGenerator jwtTokenGenerator):IRequestHandler<LoginUserCommand, Response<UserLoginDto>>
{
    public async Task<Response<UserLoginDto>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await context
            .Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);
        
        if (user is null || user.PasswordHash != request.Password)
            return Response<UserLoginDto>.Fail("Email ya da şifre hatalı.");

        var organizations = await context.OrganizationMembers
            .Where(x => x.UserId == user.Id)
            .Select(x=>new OrganizationRoleDto(x.OrganizationId, x.Role.ToString()))
            .ToListAsync(cancellationToken: cancellationToken);

        var token = jwtTokenGenerator.GenerateToken(user.Id, organizations);
        return Response<UserLoginDto>.Ok(new UserLoginDto{
            UserId = user.Id,
            Organizations = organizations,
            Token = token,
        });
    }
    
    private static string Hash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}