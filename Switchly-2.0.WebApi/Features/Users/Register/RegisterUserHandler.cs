using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Shared;
using Switchly_2._0.WebApi.Services.Helpers;

namespace Switchly_2._0.WebApi.Features.Users.Register;

public sealed record RegisterUserCommand(string Email, string Password, string Name)
    : IRequest<Response<UserRegisterDto>>;
    
public sealed record UserRegisterDto
{
    public Guid UserId { get; init; }
}
    
public class RegisterHandlerCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterHandlerCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Email is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class RegisterUserHandler(SwitchlyDbContext context)
    : IRequestHandler<RegisterUserCommand, Response<UserRegisterDto>>
{
    public async Task<Response<UserRegisterDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var checkUserExists = await context.Users.FirstOrDefaultAsync(x=>x.Email == request.Email);

        if (checkUserExists is not null)
        {
            return Response<UserRegisterDto>.Fail("Email already exists.");
        }

        var newUser = new User
        {
            Email = request.Email,
            PasswordHash = HashPasswordService.Hash(request.Password),
            Name = request.Name,
            CreatedAt = DateTime.UtcNow,
        };
        
        await context.Users.AddAsync(newUser);
        await context.SaveChangesAsync(cancellationToken);

        return Response<UserRegisterDto>.Ok(new UserRegisterDto { UserId = newUser.Id });


    }
    
    
}

    
