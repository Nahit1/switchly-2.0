using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.ProjectSettings.GetProjectSettings;

public sealed record GetProjectSettingsQuery(
    Guid OrganizationId,
    Guid ProjectId
) : IRequest<Response<List<ProjectSettingDto>>>;

public sealed record ProjectSettingDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = default!;
    public string? Description { get; set; }
    public ProjectSettingDataType DataType { get; set; }
    public bool IsSecret { get; set; }
    public List<ProjectSettingValueDto> Values { get; set; } = new();
}

public sealed record ProjectSettingValueDto
{
    public Guid ProjectEnvironmentId { get; set; }
    public string EnvironmentKey { get; set; } = default!;
    public string EnvironmentName { get; set; } = default!;
    public string? Value { get; set; }
}

public sealed class GetProjectSettingsHandler(
    SwitchlyDbContext context,
    IUserContext userContext
) : IRequestHandler<GetProjectSettingsQuery, Response<List<ProjectSettingDto>>>
{
    public async Task<Response<List<ProjectSettingDto>>> Handle(GetProjectSettingsQuery request, CancellationToken ct)
    {
        // Authorization: user must be a member of the organization
        var isMember = await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m => m.OrganizationId == request.OrganizationId && m.UserId == userContext.UserId, ct);

        if (!isMember)
            return Response<List<ProjectSettingDto>>.Fail("Bu organization için yetkin yok.");

        // Load environments for the project (dev / stg / prod)
        var envs = await context.ProjectEnvironments
            .AsNoTracking()
            .Where(e => e.ProjectId == request.ProjectId)
            .OrderBy(e => e.SortOrder)
            .Select(e => new { e.Id, e.Key, e.Name })
            .ToListAsync(ct);

        // Load settings with existing values
        var settings = await context.ProjectSettings
            .AsNoTracking()
            .Where(s => s.ProjectId == request.ProjectId)
            .OrderBy(s => s.Key)
            .Select(s => new
            {
                s.Id,
                s.Key,
                s.Description,
                s.DataType,
                s.IsSecret,
                Values = s.Values.Select(v => new
                {
                    v.ProjectEnvironmentId,
                    v.Value
                })
            })
            .ToListAsync(ct);

        // Merge settings with all environments (return null if value not defined)
        var result = settings.Select(s => new ProjectSettingDto
        {
            Id = s.Id,
            Key = s.Key,
            Description = s.Description,
            DataType = s.DataType,
            IsSecret = s.IsSecret,
            Values = envs.Select(env =>
            {
                var value = s.Values.FirstOrDefault(v => v.ProjectEnvironmentId == env.Id);
                return new ProjectSettingValueDto
                {
                    ProjectEnvironmentId = env.Id,
                    EnvironmentKey = env.Key,
                    EnvironmentName = env.Name,
                    Value = value?.Value
                };
            }).ToList()
        }).ToList();

        return Response<List<ProjectSettingDto>>.Ok(result, "Project settings");
    }
}