using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.Tracking.TrackErrors;

public sealed record TrackErrorsCommand(
    string PublicKey,
    string ProjectKey,
    string EnvironmentKey,
    List<ErrorEventInput> Events
) : IRequest<Response<TrackErrorsDto>>;

public sealed record ErrorEventInput(
    string FlagKey,
    string Severity,             // "Info" | "Warning" | "Error" | "Critical" (case-insensitive)
    string Message,
    string? Source,
    string? PropertiesJson,
    DateTimeOffset OccurredAt
);

public sealed record TrackErrorsDto(int Accepted, int Skipped);

// External monitoring tool'lar (Sentry, Datadog, custom) bu endpoint'e batch error event yollar.
// Backend ilgili flag'i bulup FlagErrorEvent yazar. RolloutGuardrailService bu tabloyu okuyup
// eşik aşılırsa schedule'ı auto-rollback eder.
public sealed class TrackErrorsHandler(SwitchlyDbContext context)
    : IRequestHandler<TrackErrorsCommand, Response<TrackErrorsDto>>
{
    private const int MaxBatchSize = 1000;

    public async Task<Response<TrackErrorsDto>> Handle(TrackErrorsCommand request, CancellationToken ct)
    {
        var publicKey = (request.PublicKey ?? string.Empty).Trim();
        var projectKey = (request.ProjectKey ?? string.Empty).Trim();
        var environmentKey = (request.EnvironmentKey ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(publicKey)) return Response<TrackErrorsDto>.Fail("PublicKey zorunludur.");
        if (string.IsNullOrWhiteSpace(projectKey)) return Response<TrackErrorsDto>.Fail("ProjectKey zorunludur.");
        if (string.IsNullOrWhiteSpace(environmentKey)) return Response<TrackErrorsDto>.Fail("EnvironmentKey zorunludur.");
        if (request.Events is null || request.Events.Count == 0)
            return Response<TrackErrorsDto>.Fail("Events listesi boş olamaz.");
        if (request.Events.Count > MaxBatchSize)
            return Response<TrackErrorsDto>.Fail($"Batch en fazla {MaxBatchSize} event içerebilir.");

        // Scope resolve (TrackExposures pattern).
        var scope = await context.ProjectEnvironments
            .AsNoTracking()
            .Where(e => e.Key == environmentKey
                        && e.Project.Key == projectKey
                        && e.Project.Organization.PublicKey == publicKey)
            .Select(e => new
            {
                OrganizationId = e.Project.OrganizationId,
                ProjectId = e.ProjectId,
                EnvironmentId = e.Id
            })
            .FirstOrDefaultAsync(ct);

        if (scope is null)
            return Response<TrackErrorsDto>.Fail("Organization/Project/Environment bulunamadı.");

        // Bulk flag resolve.
        var flagKeys = request.Events
            .Select(e => (e.FlagKey ?? string.Empty).Trim())
            .Where(k => k.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var flagMap = await context.FeatureFlags
            .AsNoTracking()
            .Where(f => f.ProjectId == scope.ProjectId && flagKeys.Contains(f.Key))
            .Select(f => new { f.Id, f.Key })
            .ToDictionaryAsync(f => f.Key, f => f.Id, StringComparer.Ordinal, ct);

        var entities = new List<FlagErrorEvent>(request.Events.Count);
        var skipped = 0;

        foreach (var ev in request.Events)
        {
            var flagKey = (ev.FlagKey ?? string.Empty).Trim();
            var message = (ev.Message ?? string.Empty).Trim();

            if (flagKey.Length == 0 || message.Length == 0) { skipped++; continue; }
            if (message.Length > 2048) message = message[..2048];

            if (!flagMap.TryGetValue(flagKey, out var flagId))
            {
                // Bilinmeyen flag — skip (fail-soft).
                skipped++;
                continue;
            }

            var severity = Enum.TryParse<ErrorSeverity>(ev.Severity, ignoreCase: true, out var s)
                ? s
                : ErrorSeverity.Error;

            entities.Add(new FlagErrorEvent
            {
                Id = Guid.NewGuid(),
                OrganizationId = scope.OrganizationId,
                ProjectId = scope.ProjectId,
                ProjectEnvironmentId = scope.EnvironmentId,
                FeatureFlagId = flagId,
                Severity = severity,
                Message = message,
                Source = ev.Source?.Trim(),
                PropertiesJson = ev.PropertiesJson,
                OccurredAt = ev.OccurredAt
            });
        }

        if (entities.Count > 0)
        {
            context.FlagErrorEvents.AddRange(entities);
            await context.SaveChangesAsync(ct);
        }

        return Response<TrackErrorsDto>.Ok(new TrackErrorsDto(entities.Count, skipped));
    }
}
