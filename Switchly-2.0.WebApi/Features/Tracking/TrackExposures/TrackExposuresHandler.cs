using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Common;

namespace Switchly_2._0.WebApi.Features.Tracking.TrackExposures;

public sealed record TrackExposuresCommand(
    string PublicKey,
    string ProjectKey,
    string EnvironmentKey,
    List<ExposureEventInput> Events
) : IRequest<Response<TrackExposuresDto>>;

public sealed record ExposureEventInput(
    string UserKey,
    string FlagKey,
    string? VariantKey,         // boolean'da veya multivariant-off durumunda null
    bool IsOn,
    DateTimeOffset OccurredAt
);

public sealed record TrackExposuresDto(int Accepted, int Skipped);

public sealed class TrackExposuresHandler(SwitchlyDbContext context)
    : IRequestHandler<TrackExposuresCommand, Response<TrackExposuresDto>>
{
    // SDK kaza yapıp sonsuz batch yollasa diye savunma. SDK'da da bound koyacağız.
    private const int MaxBatchSize = 1000;

    public async Task<Response<TrackExposuresDto>> Handle(TrackExposuresCommand request, CancellationToken ct)
    {
        var publicKey = (request.PublicKey ?? string.Empty).Trim();
        var projectKey = (request.ProjectKey ?? string.Empty).Trim();
        var environmentKey = (request.EnvironmentKey ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(publicKey))
            return Response<TrackExposuresDto>.Fail("PublicKey zorunludur.");
        if (string.IsNullOrWhiteSpace(projectKey))
            return Response<TrackExposuresDto>.Fail("ProjectKey zorunludur.");
        if (string.IsNullOrWhiteSpace(environmentKey))
            return Response<TrackExposuresDto>.Fail("EnvironmentKey zorunludur.");
        if (request.Events is null || request.Events.Count == 0)
            return Response<TrackExposuresDto>.Fail("Events listesi boş olamaz.");
        if (request.Events.Count > MaxBatchSize)
            return Response<TrackExposuresDto>.Fail($"Batch en fazla {MaxBatchSize} event içerebilir.");

        // org → project → env zincirini tek query'de ID'lere çevir.
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
            return Response<TrackExposuresDto>.Fail("Organization/Project/Environment bulunamadı.");

        // Bulk resolve: event'lerde geçen tüm flagKey'leri tek query'de ID'lerine çevir.
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

        // Bulk resolve: variant'lar. (flagId, variantKey) → variantId map'i.
        // Variant key benzersizliği flag-scope'unda; aynı key farklı flag'lerde olabilir.
        var variantMap = await context.Variants
            .AsNoTracking()
            .Where(v => flagMap.Values.Contains(v.FeatureFlagId))
            .Select(v => new { v.Id, v.Key, v.FeatureFlagId })
            .ToListAsync(ct);

        var variantLookup = variantMap.ToDictionary(
            v => (v.FeatureFlagId, v.Key.ToLowerInvariant()),
            v => v.Id);

        // Event'leri entity'ye çevir; bilinmeyen key'lerde skip (fail-soft).
        var entities = new List<FlagExposureEvent>(request.Events.Count);
        var skipped = 0;

        foreach (var ev in request.Events)
        {
            var flagKey = (ev.FlagKey ?? string.Empty).Trim();
            var userKey = (ev.UserKey ?? string.Empty).Trim();

            if (flagKey.Length == 0 || userKey.Length == 0)
            {
                skipped++;
                continue;
            }
            if (userKey.Length > 256)
            {
                skipped++;
                continue;
            }
            if (!flagMap.TryGetValue(flagKey, out var flagId))
            {
                // Consumer eski ruleset'le çalışıyor olabilir, ya da flag silinmiş. Sessizce atla.
                skipped++;
                continue;
            }

            Guid? variantId = null;
            if (!string.IsNullOrWhiteSpace(ev.VariantKey))
            {
                var lookupKey = (flagId, ev.VariantKey.Trim().ToLowerInvariant());
                if (variantLookup.TryGetValue(lookupKey, out var vid))
                {
                    variantId = vid;
                }
                else
                {
                    // Variant key gönderilmiş ama bilinmiyor — ya silindi ya yanlış yazıldı.
                    // Atmak yerine VariantId=null ile yaz: "user bu flag'i gördü, hangi variant
                    // olduğu artık bilinmiyor" bilgisi yine de değerli.
                    variantId = null;
                }
            }

            entities.Add(new FlagExposureEvent
            {
                Id = Guid.NewGuid(),
                OrganizationId = scope.OrganizationId,
                ProjectId = scope.ProjectId,
                ProjectEnvironmentId = scope.EnvironmentId,
                FeatureFlagId = flagId,
                VariantId = variantId,
                IsOn = ev.IsOn,
                UserKey = userKey,
                OccurredAt = ev.OccurredAt
            });
        }

        if (entities.Count > 0)
        {
            context.FlagExposureEvents.AddRange(entities);
            await context.SaveChangesAsync(ct);
        }

        return Response<TrackExposuresDto>.Ok(new TrackExposuresDto(entities.Count, skipped));
    }
}
