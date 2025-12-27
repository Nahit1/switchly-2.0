using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;

namespace Switchly_2._0.WebApi.Features.ProjectSettingValues.GetProjectSettingValueBySetting;

public sealed record GetProjectSettingValueBySettingQuery(
    Guid OrganizationId,
    Guid ProjectId,
    Guid ProjectSettingId
) : IRequest<Response<List<ProjectSettingValueDto>>>;

public sealed record ProjectSettingValueDto
{
    public Guid ProjectEnvironmentId { get; set; }
    public string EnvironmentKey { get; set; } = default!;
    public string EnvironmentName { get; set; } = default!;
    public string? Value { get; set; }
}

public sealed class GetProjectSettingValueBySettingHandler(
    SwitchlyDbContext context,
    IUserContext userContext
) : IRequestHandler<GetProjectSettingValueBySettingQuery, Response<List<ProjectSettingValueDto>>>
{
    public async Task<Response<List<ProjectSettingValueDto>>> Handle(
        GetProjectSettingValueBySettingQuery request,
        CancellationToken ct)
    {
        // AuthZ: organization membership
        var isMember = await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(x => x.OrganizationId == request.OrganizationId && x.UserId == userContext.UserId, ct);

        if (!isMember)
            return Response<List<ProjectSettingValueDto>>.Fail("Bu organization için yetkin yok.");

        // Boundary: setting must belong to project
        var settingOk = await context.ProjectSettings
            .AsNoTracking()
            .AnyAsync(s => s.Id == request.ProjectSettingId && s.ProjectId == request.ProjectId, ct);

        if (!settingOk)
            return Response<List<ProjectSettingValueDto>>.Fail("Project setting bulunamadı.");

        // Load all environments of the project
        var envs = await context.ProjectEnvironments
            .AsNoTracking()
            .Where(e => e.ProjectId == request.ProjectId)
            .OrderBy(e => e.SortOrder)
            .Select(e => new { e.Id, e.Key, e.Name })
            .ToListAsync(ct);

        // Load existing values for this setting
        var values = await context.ProjectSettingValues
            .AsNoTracking()
            .Where(v => v.ProjectSettingId == request.ProjectSettingId)
            .Select(v => new { v.ProjectEnvironmentId, v.Value })
            .ToListAsync(ct);

        // Merge envs with values (null if not exists)
        var result = envs.Select(env =>
        {
            var val = values.FirstOrDefault(v => v.ProjectEnvironmentId == env.Id);
            return new ProjectSettingValueDto
            {
                ProjectEnvironmentId = env.Id,
                EnvironmentKey = env.Key,
                EnvironmentName = env.Name,
                Value = val?.Value
            };
        }).ToList();

        return Response<List<ProjectSettingValueDto>>.Ok(result, "Project setting values");
    }
}