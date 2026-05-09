using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;

namespace Switchly_2._0.WebApi.Features.ProjectSettings.EvaluateProjectSettings;

public sealed record EvaluateProjectSettingsRequest(
    string PublicKey,
    string ProjectKey,
    string EnvironmentKey
) : IRequest<Response<EvaluateProjectSettingsResponseDto>>;

public sealed record EvaluateProjectSettingsResponseDto(
    IReadOnlyDictionary<string, string?> Values
);

public sealed class EvaluateProjectSettingsHandler(SwitchlyDbContext context)
    : IRequestHandler<EvaluateProjectSettingsRequest, Response<EvaluateProjectSettingsResponseDto>>
{
    public async Task<Response<EvaluateProjectSettingsResponseDto>> Handle(
        EvaluateProjectSettingsRequest request,
        CancellationToken ct)
    {
        var publicKey = (request.PublicKey ?? string.Empty).Trim();
        var projectKey = (request.ProjectKey ?? string.Empty).Trim();
        var environmentKey = (request.EnvironmentKey ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(publicKey))
            return Response<EvaluateProjectSettingsResponseDto>.Fail("PublicKey zorunludur.");

        if (string.IsNullOrWhiteSpace(projectKey))
            return Response<EvaluateProjectSettingsResponseDto>.Fail("ProjectKey zorunludur.");

        if (string.IsNullOrWhiteSpace(environmentKey))
            return Response<EvaluateProjectSettingsResponseDto>.Fail("EnvironmentKey zorunludur.");

        var org = await context.Organizations
            .AsNoTracking()
            .Where(o => o.PublicKey == publicKey)
            .Select(o => new { o.Id })
            .FirstOrDefaultAsync(ct);

        if (org is null)
            return Response<EvaluateProjectSettingsResponseDto>.Fail("Organization bulunamadı.");

        var project = await context.Projects
            .AsNoTracking()
            .Where(p => p.OrganizationId == org.Id && p.Key == projectKey)
            .Select(p => new { p.Id })
            .FirstOrDefaultAsync(ct);

        if (project is null)
            return Response<EvaluateProjectSettingsResponseDto>.Fail("Project bulunamadı.");

        var env = await context.ProjectEnvironments
            .AsNoTracking()
            .Where(e => e.ProjectId == project.Id && e.Key == environmentKey)
            .Select(e => new { e.Id })
            .FirstOrDefaultAsync(ct);

        if (env is null)
            return Response<EvaluateProjectSettingsResponseDto>.Fail("Environment bulunamadı.");

        // IsSecret olanları anonim PublicKey ile dönmüyoruz.
        // Değeri olmayan ayarları sözlüğe koymuyoruz; client TryGetValue ile kontrol etsin.
        var rows = await context.ProjectSettings
            .AsNoTracking()
            .Where(s => s.ProjectId == project.Id && !s.IsSecret)
            .Select(s => new
            {
                s.Key,
                Value = s.Values
                    .Where(v => v.ProjectEnvironmentId == env.Id)
                    .Select(v => v.Value)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var values = rows
            .Where(r => r.Value is not null)
            .ToDictionary(r => r.Key, r => r.Value);

        return Response<EvaluateProjectSettingsResponseDto>.Ok(
            new EvaluateProjectSettingsResponseDto(values));
    }
}
