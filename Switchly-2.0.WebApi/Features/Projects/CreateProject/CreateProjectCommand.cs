using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.Projects.CreateProject;
public sealed record CreateProjectCommand
    (Guid OrganizationId, string name, string description): IRequest<Response<CreateProjectDto>>;

public sealed record CreateProjectDto
{
    public Guid ProjectId { get; set; }
}


public class CreateOrganizationHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<CreateProjectCommand, Response<CreateProjectDto>>
{
    public async Task<Response<CreateProjectDto>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var userId = userContext.UserId;
        
        var user = await context.Users.Include(user => user.OrganizationMembers).FirstOrDefaultAsync(x=>x.Id ==userId, cancellationToken);
        if (user is null)
        {
            return Response<CreateProjectDto>.Fail("User does not exist");
        }

        if (user.OrganizationMembers.Any(x => x.Role != OrganizationRole.Admin && x.Role != OrganizationRole.Owner))
        {
            return Response<CreateProjectDto>.Fail("User has no permission to create a project");
        }
        
        throw new NotImplementedException();
    }
}

