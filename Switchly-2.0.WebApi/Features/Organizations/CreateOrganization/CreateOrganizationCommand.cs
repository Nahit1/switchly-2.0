using MediatR;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;
using Switchly_2._0.WebApi.Services.Helpers;

namespace Switchly_2._0.WebApi.Features.Organizations.CreateOrganization;

public sealed record CreateOrganizationCommand(string Name): IRequest<Response<CreateOrganizationDto>>;

public sealed record CreateOrganizationDto
{
    public string PublicKey { get; init; }
    public string Slug { get; set; }
}

public class CreateOrganizationHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<CreateOrganizationCommand, Response<CreateOrganizationDto>>
{
    public async Task<Response<CreateOrganizationDto>> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var checkExists = context.Organizations.FirstOrDefault(x => x.Name == request.Name);
        if (checkExists is not null)
        {
            return Response<CreateOrganizationDto>.Fail("This organization already exists.");
        }
        
        var userId = userContext.UserId;
        
        var org = new Organization
        {
            Name = request.Name,
            Slug = SlugHelper.Slugify(request.Name),
            PublicKey = GenerateOrganizationKey.GenerateKey(),
            CreatedAt = DateTime.UtcNow
        };
        
        
        
        await context.Organizations.AddAsync(org);
        
        var member = new OrganizationMember
        {
            OrganizationId = org.Id,
            UserId = userId,
            Role = OrganizationRole.Owner,
            CreatedAt = DateTime.UtcNow
        };
        
        await context.OrganizationMembers.AddAsync(member, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        
        

        return Response<CreateOrganizationDto>.Ok(new CreateOrganizationDto
        {
            PublicKey = org.PublicKey,
            Slug = org.Slug,
        });
    }
}