using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;

namespace Switchly_2._0.WebApi.Features.Tracking.GetConversionEventNames;

public sealed record GetConversionEventNamesQuery(
    Guid ProjectId,
    DateTimeOffset Since
) : IRequest<Response<List<ConversionEventNameDto>>>;

public sealed record ConversionEventNameDto(
    string EventName,
    int Count
);

// Dashboard'da conversion event ismini elle yazmak yerine, son 30 günde projeye düşmüş
// distinct event isimleri + sıklıklaşma. UI bunu dropdown olarak gösterir.
public sealed class GetConversionEventNamesHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<GetConversionEventNamesQuery, Response<List<ConversionEventNameDto>>>
{
    public async Task<Response<List<ConversionEventNameDto>>> Handle(
        GetConversionEventNamesQuery request, CancellationToken ct)
    {
        // Org üyeliği kontrolü.
        var project = await context.Projects
            .AsNoTracking()
            .Where(p => p.Id == request.ProjectId)
            .Select(p => new { p.Id, p.OrganizationId })
            .FirstOrDefaultAsync(ct);

        if (project is null)
            return Response<List<ConversionEventNameDto>>.Fail("Project bulunamadı.");

        var isMember = await context.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == project.OrganizationId
                           && m.UserId == userContext.UserId, ct);

        if (!isMember)
            throw new UnauthorizedAccessException("Bu organization için yetkin yok.");

        // GroupBy + record constructor projection EF Core'da çevrilmiyor — anonymous type'a
        // toplayıp materialize ettikten sonra DTO'ya map'liyoruz (LINQ to Objects).
        var raw = await context.ConversionEvents
            .AsNoTracking()
            .Where(c => c.ProjectId == request.ProjectId && c.OccurredAt >= request.Since)
            .GroupBy(c => c.EventName)
            .Select(g => new { EventName = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var names = raw
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.EventName, StringComparer.Ordinal)
            .Select(x => new ConversionEventNameDto(x.EventName, x.Count))
            .ToList();

        return Response<List<ConversionEventNameDto>>.Ok(names);
    }
}
