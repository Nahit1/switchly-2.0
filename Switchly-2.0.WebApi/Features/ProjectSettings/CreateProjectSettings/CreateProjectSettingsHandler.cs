using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.ProjectSettings.CreateProjectSettings;

public sealed record CreateProjectSettingCommand(
    Guid OrganizationId,
    Guid ProjectId,
    string Key,
    string? Description,
    ProjectSettingDataType DataType,
    bool IsSecret
) : IRequest<Response<Guid>>;

public sealed class CreateProjectSettingsHandler(
    SwitchlyDbContext context,
    IUserContext userContext
) : IRequestHandler<CreateProjectSettingCommand, Response<Guid>>
{
    public async Task<Response<Guid>> Handle(CreateProjectSettingCommand request, CancellationToken ct)
    {
        // AuthZ: user must be a member of the organization
        var isMember = await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m => m.OrganizationId == request.OrganizationId && m.UserId == userContext.UserId, ct);

        if (!isMember)
            return Response<Guid>.Fail("Bu organization için yetkin yok.");

        // Boundary guard: project must belong to the organization
        var projectOk = await context.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProjectId && p.OrganizationId == request.OrganizationId, ct);

        if (!projectOk)
            return Response<Guid>.Fail("Project bulunamadı.");

        var key = (request.Key ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(key))
            return Response<Guid>.Fail("Key zorunludur.");

        // Uniqueness within project
        var exists = await context.ProjectSettings
            .AsNoTracking()
            .AnyAsync(s => s.ProjectId == request.ProjectId && s.Key == key, ct);

        if (exists)
            return Response<Guid>.Fail("Bu key ile daha önce setting tanımlanmış.");

        var now = DateTimeOffset.UtcNow;

        var entity = new ProjectSetting
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Key = key,
            Description = request.Description?.Trim(),
            DataType = request.DataType,
            IsSecret = request.IsSecret,
            CreatedAt = now,
        };

        context.ProjectSettings.Add(entity);
        await context.SaveChangesAsync(ct);

        return Response<Guid>.Ok(entity.Id, "Setting created");
    }
}