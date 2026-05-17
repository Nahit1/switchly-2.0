using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;

namespace Switchly_2._0.WebApi.Features.Tracking.GetFlagExposureStats;

public sealed record GetFlagExposureStatsQuery(
    Guid FlagId,
    Guid? EnvironmentId,
    DateTimeOffset Since
) : IRequest<Response<FlagExposureStatsDto>>;

public sealed record FlagExposureStatsDto(
    Guid FlagId,
    string FlagKey,
    DateTimeOffset Since,
    int TotalExposures,
    int UniqueUsers,
    List<VariantExposureStatsDto> Variants
);

public sealed record VariantExposureStatsDto(
    Guid? VariantId,
    string? VariantKey,
    bool IsOn,
    int TotalExposures,
    int UniqueUsers
);

public sealed class GetFlagExposureStatsHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<GetFlagExposureStatsQuery, Response<FlagExposureStatsDto>>
{
    public async Task<Response<FlagExposureStatsDto>> Handle(GetFlagExposureStatsQuery request, CancellationToken ct)
    {
        // Flag'i bul + organizasyon üyeliği kontrolü.
        var flag = await context.FeatureFlags
            .AsNoTracking()
            .Where(f => f.Id == request.FlagId)
            .Select(f => new { f.Id, f.Key, f.Project.OrganizationId })
            .FirstOrDefaultAsync(ct);

        if (flag is null)
            return Response<FlagExposureStatsDto>.Fail("Flag bulunamadı.");

        var isMember = await context.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == flag.OrganizationId
                           && m.UserId == userContext.UserId, ct);

        if (!isMember)
            throw new UnauthorizedAccessException("Bu organization için yetkin yok.");

        // Event sorgusunun temel filter'ı — env opsiyonel.
        var baseQuery = context.FlagExposureEvents
            .AsNoTracking()
            .Where(e => e.FeatureFlagId == request.FlagId
                        && e.OccurredAt >= request.Since);

        if (request.EnvironmentId.HasValue)
            baseQuery = baseQuery.Where(e => e.ProjectEnvironmentId == request.EnvironmentId.Value);

        // Variant başına aggregate (4 olası outcome kovası: variant+isOn permutasyonları).
        var aggregates = await baseQuery
            .GroupBy(e => new { e.VariantId, e.IsOn })
            .Select(g => new
            {
                g.Key.VariantId,
                g.Key.IsOn,
                TotalExposures = g.Count(),
                UniqueUsers = g.Select(e => e.UserKey).Distinct().Count()
            })
            .ToListAsync(ct);

        // Variant key'leri tek query'de resolve et (raw aggregate sadece Id taşıyor).
        var variantIds = aggregates
            .Where(a => a.VariantId.HasValue)
            .Select(a => a.VariantId!.Value)
            .Distinct()
            .ToList();

        var variantKeys = variantIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await context.Variants
                .AsNoTracking()
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => v.Key, ct);

        var variants = aggregates
            .Select(a => new VariantExposureStatsDto(
                VariantId: a.VariantId,
                VariantKey: a.VariantId.HasValue && variantKeys.TryGetValue(a.VariantId.Value, out var k)
                    ? k
                    : null,
                IsOn: a.IsOn,
                TotalExposures: a.TotalExposures,
                UniqueUsers: a.UniqueUsers
            ))
            .OrderByDescending(v => v.UniqueUsers)
            .ThenBy(v => v.VariantKey)
            .ToList();

        var totalExposures = variants.Sum(v => v.TotalExposures);

        // Ayrı sorgu: aynı user variant değiştirebileceği için cross-variant unique
        // count'u group toplamı değil; baseQuery üzerinden tek COUNT(DISTINCT UserKey).
        var uniqueUsers = await baseQuery
            .Select(e => e.UserKey)
            .Distinct()
            .CountAsync(ct);

        return Response<FlagExposureStatsDto>.Ok(new FlagExposureStatsDto(
            FlagId: flag.Id,
            FlagKey: flag.Key,
            Since: request.Since,
            TotalExposures: totalExposures,
            UniqueUsers: uniqueUsers,
            Variants: variants
        ));
    }
}
