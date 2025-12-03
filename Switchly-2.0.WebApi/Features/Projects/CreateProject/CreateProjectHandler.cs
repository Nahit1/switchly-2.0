using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.Projects.CreateProject;
public sealed record CreateProjectHandler
    (Guid OrganizationId, string name, string description): IRequest<Response<CreateProjectDto>>;

public sealed record CreateProjectDto
{
    public Guid ProjectId { get; set; }
}


public class CreateOrganizationHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<CreateProjectHandler, Response<CreateProjectDto>>
{
    public async Task<Response<CreateProjectDto>> Handle(CreateProjectHandler request, CancellationToken cancellationToken)
    {
        var userId = userContext.UserId;
        
        var user = await context.Users
            .Include(user => user.OrganizationMembers)
            .AsNoTracking()
            .FirstOrDefaultAsync(x=>x.Id ==userId, cancellationToken);

        var existsProjectName = await context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == request.name, cancellationToken: cancellationToken);
        
        if (existsProjectName is not null)
        {
            return Response<CreateProjectDto>.Fail("Project name already exists");
        }
        if (user is null)
        {
            return Response<CreateProjectDto>.Fail("User does not exist");
        }

        if (user.OrganizationMembers.Any(x => x.Role != OrganizationRole.Admin && x.Role != OrganizationRole.Owner))
        {
            return Response<CreateProjectDto>.Fail("User has no permission to create a project");
        }

        var project = new Project
        {
            OrganizationId = request.OrganizationId,
            Name = request.name,
            Key = Guid.Empty.ToString(),
            Description = request.description,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
        };
        
        await context.Projects.AddAsync(project, cancellationToken);
        if (await context.SaveChangesAsync(cancellationToken) > 0)
        {
            return Response<CreateProjectDto>.Ok(new CreateProjectDto{ProjectId = project.Id},"Project created");
        }
        
        return Response<CreateProjectDto>.Fail("Failed to create project");
    }
}

