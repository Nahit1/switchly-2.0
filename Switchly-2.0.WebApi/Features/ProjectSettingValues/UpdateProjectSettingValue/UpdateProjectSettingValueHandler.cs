using System.Globalization;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.ProjectSettingValues.UpdateProjectSettingValue;

public sealed record UpdateProjectSettingValueCommand(
    Guid OrganizationId,
    Guid ProjectId,
    Guid ProjectSettingId,
    Guid ProjectEnvironmentId,
    string? Value
) : IRequest<Response<Guid>>;

public sealed class UpdateProjectSettingValueHandler(
    SwitchlyDbContext context,
    IUserContext userContext
) : IRequestHandler<UpdateProjectSettingValueCommand, Response<Guid>>
{
    public async Task<Response<Guid>> Handle(UpdateProjectSettingValueCommand request, CancellationToken ct)
    {
        var isMember = await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(x => x.OrganizationId == request.OrganizationId && x.UserId == userContext.UserId, ct);

        if (!isMember)
            return Response<Guid>.Fail("Bu organization için yetkin yok.");

        var setting = await context.ProjectSettings
            .AsNoTracking()
            .Where(s => s.Id == request.ProjectSettingId && s.ProjectId == request.ProjectId)
            .Select(s => new { s.Id, s.DataType })
            .FirstOrDefaultAsync(ct);

        if (setting is null)
            return Response<Guid>.Fail("Project setting bulunamadı.");

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
                    if (!decimal.TryParse(request.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                        return Response<Guid>.Fail("Geçersiz sayı formatı.");
                    break;

                case ProjectSettingDataType.Boolean:
                    var v = request.Value.Trim();
                    var isBool = bool.TryParse(v, out _);
                    var is01 = v == "1" || v == "0";
                    if (!isBool && !is01)
                        return Response<Guid>.Fail("Geçersiz boolean formatı. true/false veya 1/0 kullanın.");
                    break;

                default:
                    break;
            }
        }

        var envOk = await context.ProjectEnvironments
            .AsNoTracking()
            .AnyAsync(e => e.Id == request.ProjectEnvironmentId && e.ProjectId == request.ProjectId, ct);

        if (!envOk)
            return Response<Guid>.Fail("Project environment bulunamadı.");

        var entity = await context.ProjectSettingValues
            .FirstOrDefaultAsync(v =>
                v.ProjectSettingId == request.ProjectSettingId &&
                v.ProjectEnvironmentId == request.ProjectEnvironmentId, ct);

        if (entity is null)
            return Response<Guid>.Fail("Bu environment için setting değeri bulunamadı. Önce oluşturun.");

        entity.Value = request.Value;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(ct);

        return Response<Guid>.Ok(entity.Id, "Setting value updated");
    }
}
