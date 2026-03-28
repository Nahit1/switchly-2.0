using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Features.ProjectSettings.GetProjectSettings;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.ProjectSettings.GetProjectSettingsByEnviroment;

public sealed record GetProjectSettingsByEnvironmentQuery(
    Guid OrganizationId,
    Guid EnvironmentId,
    Guid ProjectId
) : IRequest<Response<List<ProjectSettingByEnvironmentDto>>>;

public sealed record ProjectSettingByEnvironmentDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = default!;
    public string? Description { get; set; }
    public ProjectSettingDataType DataType { get; set; }
    public bool IsSecret { get; set; }
    public ProjectSettingValueDto? Value { get; set; }
}

public sealed record ProjectSettingValueDto
{
    public Guid Id { get; set; }
    public string? Value { get; set; }
}

public sealed class GetProjectSettingsByEnviromentHandler(
    SwitchlyDbContext context,
    IUserContext userContext
): IRequestHandler<GetProjectSettingsByEnvironmentQuery, Response<List<ProjectSettingByEnvironmentDto>>>
{
    public async Task<Response<List<ProjectSettingByEnvironmentDto>>> Handle(GetProjectSettingsByEnvironmentQuery request, CancellationToken cancellationToken)
    {
        // Authorization: user must be a member of the organization
        var isMember = await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m => m.OrganizationId == request.OrganizationId && m.UserId == userContext.UserId, cancellationToken);

        if (!isMember)
            return Response<List<ProjectSettingByEnvironmentDto>>.Fail("Bu organization için yetkin yok.");
        
        var settings = await context.ProjectSettings
            .AsNoTracking()
            .Where(s => s.ProjectId == request.ProjectId)
            .OrderBy(s => s.Key)
            .Select(s => new ProjectSettingByEnvironmentDto
            {
                Id = s.Id,
                Key = s.Key,
                Description = s.Description,
                DataType = s.DataType,
                IsSecret = s.IsSecret,
                Value = s.Values
                    .Where(v => v.ProjectEnvironmentId == request.EnvironmentId)
                    .Select(v => new ProjectSettingValueDto
                    {
                        Id = v.Id,
                        Value = v.Value
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
        
        return Response<List<ProjectSettingByEnvironmentDto>>.Ok(settings, "Project settings");
    }
}
