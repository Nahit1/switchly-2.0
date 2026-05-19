using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Observability;

namespace Switchly_2._0.WebApi.Features.Tracking.TrackConversions;

public sealed record TrackConversionsCommand(
    string PublicKey,
    string ProjectKey,
    string EnvironmentKey,
    List<ConversionEventInput> Events
) : IRequest<Response<TrackConversionsDto>>;

public sealed record ConversionEventInput(
    string UserKey,
    string EventName,
    decimal? Value,
    string? PropertiesJson,
    DateTimeOffset OccurredAt
);

public sealed record TrackConversionsDto(int Accepted, int Skipped);

public sealed class TrackConversionsHandler(SwitchlyDbContext context)
    : IRequestHandler<TrackConversionsCommand, Response<TrackConversionsDto>>
{
    private const int MaxBatchSize = 1000;

    public async Task<Response<TrackConversionsDto>> Handle(TrackConversionsCommand request, CancellationToken ct)
    {
        using var activity = SwitchlyActivitySources.Tracking.StartActivity("TrackConversions.Ingest");
        activity?.SetTag("incoming_events", request.Events?.Count ?? 0);

        var publicKey = (request.PublicKey ?? string.Empty).Trim();
        var projectKey = (request.ProjectKey ?? string.Empty).Trim();
        var environmentKey = (request.EnvironmentKey ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(publicKey))
            return Response<TrackConversionsDto>.Fail("PublicKey zorunludur.");
        if (string.IsNullOrWhiteSpace(projectKey))
            return Response<TrackConversionsDto>.Fail("ProjectKey zorunludur.");
        if (string.IsNullOrWhiteSpace(environmentKey))
            return Response<TrackConversionsDto>.Fail("EnvironmentKey zorunludur.");
        if (request.Events is null || request.Events.Count == 0)
            return Response<TrackConversionsDto>.Fail("Events listesi boş olamaz.");
        if (request.Events.Count > MaxBatchSize)
            return Response<TrackConversionsDto>.Fail($"Batch en fazla {MaxBatchSize} event içerebilir.");

        // Org → project → env zincirini tek query'de ID'lere çevir (TrackExposures ile aynı pattern).
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
            return Response<TrackConversionsDto>.Fail("Organization/Project/Environment bulunamadı.");

        // Conversion flag-agnostic; flag/variant resolve'a gerek yok. Doğrudan event listesini map'le.
        var entities = new List<ConversionEvent>(request.Events.Count);
        var skipped = 0;

        foreach (var ev in request.Events)
        {
            var userKey = (ev.UserKey ?? string.Empty).Trim();
            var eventName = (ev.EventName ?? string.Empty).Trim();

            if (userKey.Length == 0 || eventName.Length == 0)
            {
                skipped++;
                continue;
            }
            if (userKey.Length > 256 || eventName.Length > 128)
            {
                skipped++;
                continue;
            }

            entities.Add(new ConversionEvent
            {
                Id = Guid.NewGuid(),
                OrganizationId = scope.OrganizationId,
                ProjectId = scope.ProjectId,
                ProjectEnvironmentId = scope.EnvironmentId,
                UserKey = userKey,
                EventName = eventName,
                Value = ev.Value,
                PropertiesJson = ev.PropertiesJson,
                OccurredAt = ev.OccurredAt
            });
        }

        if (entities.Count > 0)
        {
            context.ConversionEvents.AddRange(entities);
            await context.SaveChangesAsync(ct);
        }

        activity?.SetTag("accepted", entities.Count);
        activity?.SetTag("skipped", skipped);

        return Response<TrackConversionsDto>.Ok(new TrackConversionsDto(entities.Count, skipped));
    }
}
