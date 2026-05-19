using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;

namespace Switchly_2._0.WebApi.Features.Tracking.GetFlagExposureTimeline;

public sealed record GetFlagExposureTimelineQuery(
    Guid FlagId,
    Guid? EnvironmentId,
    DateTimeOffset Since,
    TimelineBucketSize BucketSize
) : IRequest<Response<FlagExposureTimelineDto>>;

public enum TimelineBucketSize { Hour = 1, Day = 2 }

public sealed record FlagExposureTimelineDto(
    Guid FlagId,
    string FlagKey,
    string BucketSize,
    DateTimeOffset Since,
    List<TimelineBucketDto> Buckets,
    List<TimelineVariantDto> Variants  // variant key/id sözlüğü, UI'da legend için
);

public sealed record TimelineBucketDto(
    DateTimeOffset Bucket,
    List<TimelineBucketVariantDto> Variants
);

public sealed record TimelineBucketVariantDto(
    Guid? VariantId,
    bool IsOn,
    int Exposures
);

public sealed record TimelineVariantDto(
    Guid? VariantId,
    string? VariantKey,
    bool IsOn,
    int SortOrder
);

// Saatlik veya günlük bucket'lara exposure count'u dağıtır. Trend grafiği için temel veri.
// Bucket sayısı: 1h/24h aralığı → saatlik = 24'e kadar; 7d/30d → günlük = 30'a kadar. Hep ufak.
public sealed class GetFlagExposureTimelineHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<GetFlagExposureTimelineQuery, Response<FlagExposureTimelineDto>>
{
    public async Task<Response<FlagExposureTimelineDto>> Handle(
        GetFlagExposureTimelineQuery request, CancellationToken ct)
    {
        var flag = await context.FeatureFlags
            .AsNoTracking()
            .Where(f => f.Id == request.FlagId)
            .Select(f => new { f.Id, f.Key, f.Project.OrganizationId })
            .FirstOrDefaultAsync(ct);

        if (flag is null)
            return Response<FlagExposureTimelineDto>.Fail("Flag bulunamadı.");

        var isMember = await context.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == flag.OrganizationId
                           && m.UserId == userContext.UserId, ct);

        if (!isMember)
            throw new UnauthorizedAccessException("Bu organization için yetkin yok.");

        // Ham event'leri çek (sadece bucket için gereken kolonlar).
        var baseQuery = context.FlagExposureEvents
            .AsNoTracking()
            .Where(e => e.FeatureFlagId == request.FlagId && e.OccurredAt >= request.Since);

        if (request.EnvironmentId.HasValue)
            baseQuery = baseQuery.Where(e => e.ProjectEnvironmentId == request.EnvironmentId.Value);

        var rows = await baseQuery
            .Select(e => new { e.OccurredAt, e.VariantId, e.IsOn })
            .ToListAsync(ct);

        // In-memory bucketing — DateTrunc EF Core'da provider-spesifik, in-memory daha portable
        // ve volume küçük (saatlik 24 bucket × ~5 variant outcome = ~120 grup).
        var bucketed = rows
            .GroupBy(r => new
            {
                Bucket = TruncateToBucket(r.OccurredAt, request.BucketSize),
                r.VariantId,
                r.IsOn
            })
            .Select(g => new
            {
                g.Key.Bucket,
                g.Key.VariantId,
                g.Key.IsOn,
                Count = g.Count()
            })
            .ToList();

        // Variant key'lerini resolve et.
        var variantIds = bucketed
            .Where(b => b.VariantId.HasValue)
            .Select(b => b.VariantId!.Value)
            .Distinct()
            .ToList();

        var variantInfoMap = variantIds.Count == 0
            ? new Dictionary<Guid, (string Key, int SortOrder)>()
            : await context.Variants
                .AsNoTracking()
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => (v.Key, v.SortOrder), ct);

        // Variants sözlüğü (UI legend'i için).
        var allVariants = bucketed
            .Select(b => (b.VariantId, b.IsOn))
            .Distinct()
            .Select(t => new TimelineVariantDto(
                VariantId: t.VariantId,
                VariantKey: t.VariantId.HasValue && variantInfoMap.TryGetValue(t.VariantId.Value, out var info)
                    ? info.Key
                    : null,
                IsOn: t.IsOn,
                SortOrder: t.VariantId.HasValue && variantInfoMap.TryGetValue(t.VariantId.Value, out var info2)
                    ? info2.SortOrder
                    : int.MaxValue
            ))
            .OrderBy(v => v.SortOrder)
            .ThenBy(v => v.VariantKey)
            .ToList();

        // Bucket'ları zamana göre sırala ve grupla.
        var bucketGroups = bucketed
            .GroupBy(b => b.Bucket)
            .OrderBy(g => g.Key)
            .Select(g => new TimelineBucketDto(
                Bucket: g.Key,
                Variants: g.Select(x => new TimelineBucketVariantDto(
                    VariantId: x.VariantId,
                    IsOn: x.IsOn,
                    Exposures: x.Count
                )).ToList()
            ))
            .ToList();

        return Response<FlagExposureTimelineDto>.Ok(new FlagExposureTimelineDto(
            FlagId: flag.Id,
            FlagKey: flag.Key,
            BucketSize: request.BucketSize.ToString().ToLowerInvariant(),
            Since: request.Since,
            Buckets: bucketGroups,
            Variants: allVariants
        ));
    }

    private static DateTimeOffset TruncateToBucket(DateTimeOffset value, TimelineBucketSize bucketSize)
        => bucketSize switch
        {
            TimelineBucketSize.Day => new DateTimeOffset(value.Year, value.Month, value.Day, 0, 0, 0, TimeSpan.Zero),
            _ => new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, 0, 0, TimeSpan.Zero),
        };
}
