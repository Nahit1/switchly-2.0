using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.SegmetGroups.GetAllSegmentGroups;

public sealed record GetAllSegmentGroupsQuery(
    Guid OrganizationId
) : IRequest<Response<List<SegmentGroupDto>>>;

public sealed record SegmentGroupDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public LogicalOperator LogicalOperator { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class GetAllSegmentGroupHandler(
    SwitchlyDbContext context,
    IUserContext userContext
) : IRequestHandler<GetAllSegmentGroupsQuery, Response<List<SegmentGroupDto>>>
{
    public async Task<Response<List<SegmentGroupDto>>> Handle(GetAllSegmentGroupsQuery request, CancellationToken ct)
    {
        // AuthZ: user must be a member of the organization
        var isMember = await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m => m.OrganizationId == request.OrganizationId && m.UserId == userContext.UserId, ct);

        if (!isMember)
            return Response<List<SegmentGroupDto>>.Fail("Bu organization için yetkin yok.");

        var items = await context.SegmentGroups
            .AsNoTracking()
            .Where(x => x.OrganizationId == request.OrganizationId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new SegmentGroupDto
            {
                Id = x.Id,
                Key = x.Key,
                Name = x.Name,
                Description = x.Description,
                LogicalOperator = x.LogicalOperator,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(ct);

        return Response<List<SegmentGroupDto>>.Ok(items, "Segment groups");
    }
}