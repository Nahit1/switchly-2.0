using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Globalization;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.ProjectSettingValues.CreateProjectSettingsValue;

public sealed record CreateProjectSettingValueCommand(
    Guid OrganizationId,
    Guid ProjectId,
    Guid ProjectSettingId,
    Guid ProjectEnvironmentId,
    string? Value
) : IRequest<Response<Guid>>;

public sealed class CreateProjectSettingValuesHandler(
    SwitchlyDbContext context,
    IUserContext userContext
) : IRequestHandler<CreateProjectSettingValueCommand, Response<Guid>>
{
    public async Task<Response<Guid>> Handle(CreateProjectSettingValueCommand request, CancellationToken ct)
    {
        // AuthZ: organization membership
        var isMember = await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(x => x.OrganizationId == request.OrganizationId && x.UserId == userContext.UserId, ct);

        if (!isMember)
            return Response<Guid>.Fail("Bu organization için yetkin yok.");

        // Boundary: setting project (and read DataType)
        var setting = await context.ProjectSettings
            .AsNoTracking()
            .Where(s => s.Id == request.ProjectSettingId && s.ProjectId == request.ProjectId)
            .Select(s => new { s.Id, s.DataType })
            .FirstOrDefaultAsync(ct);

        if (setting is null)
            return Response<Guid>.Fail("Project setting bulunamadı.");

        // MVP validation: validate value based on DataType (when not empty)
        if (!string.IsNullOrWhiteSpace(request.Value))
        {
            switch (setting.DataType)
            {
                case ProjectSettingDataType.Json:
                    try
                    {
                        JsonDocument.Parse(request.Value);
                    }
                    catch
                    {
                        return Response<Guid>.Fail("Geçersiz JSON formatı.");
                    }
                    break;

                case ProjectSettingDataType.Number:
                    // Accept integers/decimals using invariant culture (e.g., 3 or 3.14)
                    if (!decimal.TryParse(request.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                        return Response<Guid>.Fail("Geçersiz sayı formatı.");
                    break;

                case ProjectSettingDataType.Boolean:
                    // Accept true/false (case-insensitive) and 1/0
                    var v = request.Value.Trim();
                    var isBool = bool.TryParse(v, out _);
                    var is01 = v == "1" || v == "0";
                    if (!isBool && !is01)
                        return Response<Guid>.Fail("Geçersiz boolean formatı. true/false veya 1/0 kullanın.");
                    break;

                default:
                    // String or other types: no extra validation in MVP
                    break;
            }
        }

        // Boundary: environment project
        var envOk = await context.ProjectEnvironments
            .AsNoTracking()
            .AnyAsync(e => e.Id == request.ProjectEnvironmentId && e.ProjectId == request.ProjectId, ct);

        if (!envOk)
            return Response<Guid>.Fail("Project environment bulunamadı.");

        // Prevent duplicate create (update will be added later)
        var exists = await context.ProjectSettingValues
            .AsNoTracking()
            .AnyAsync(v =>
                v.ProjectSettingId == request.ProjectSettingId &&
                v.ProjectEnvironmentId == request.ProjectEnvironmentId, ct);

        if (exists)
            return Response<Guid>.Fail("Bu environment için setting değeri zaten mevcut.");

        var entity = new ProjectSettingValue
        {
            Id = Guid.NewGuid(),
            ProjectSettingId = request.ProjectSettingId,
            ProjectEnvironmentId = request.ProjectEnvironmentId,
            Value = request.Value,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        context.ProjectSettingValues.Add(entity);
        await context.SaveChangesAsync(ct);

        return Response<Guid>.Ok(entity.Id, "Setting value created");
    }
}