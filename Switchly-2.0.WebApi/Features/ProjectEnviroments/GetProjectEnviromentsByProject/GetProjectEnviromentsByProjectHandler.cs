using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;

namespace Switchly_2._0.WebApi.Features.ProjectEnviroments.GetProjectEnviromentsByProject;

public sealed record GetProjectEnviromentsByProjectQuery(
    Guid OrganizationId,
    Guid ProjectId
) : IRequest<Response<List<ProjectEnvironmentDto>>>;

public sealed record ProjectEnvironmentDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class GetProjectEnviromentsByProjectHandler(
    SwitchlyDbContext context,
    IUserContext userContext
) : IRequestHandler<GetProjectEnviromentsByProjectQuery, Response<List<ProjectEnvironmentDto>>>
{
    public async Task<Response<List<ProjectEnvironmentDto>>> Handle(GetProjectEnviromentsByProjectQuery request, CancellationToken ct)
    {
        // AuthZ: user must be a member of the organization
        var isMember = await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m => m.OrganizationId == request.OrganizationId && m.UserId == userContext.UserId, ct);

        if (!isMember)
            return Response<List<ProjectEnvironmentDto>>.Fail("Bu organization için yetkin yok.");

        // Boundary: project must belong to org
        var projectOk = await context.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProjectId && p.OrganizationId == request.OrganizationId, ct);

        if (!projectOk)
            return Response<List<ProjectEnvironmentDto>>.Fail("Project bulunamadı.");

        var envs = await context.ProjectEnvironments
            .AsNoTracking()
            .Where(e => e.ProjectId == request.ProjectId)
            .OrderBy(e => e.SortOrder)
            .Select(e => new ProjectEnvironmentDto
            {
                Id = e.Id,
                Key = e.Key,
                Name = e.Name,
                SortOrder = e.SortOrder,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync(ct);

        return Response<List<ProjectEnvironmentDto>>.Ok(envs, "Project environments");
    }
}