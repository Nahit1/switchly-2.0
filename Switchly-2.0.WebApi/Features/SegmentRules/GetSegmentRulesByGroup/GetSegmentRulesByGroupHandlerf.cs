using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.SegmentRules.GetSegmentRulesByGroup;

public sealed record GetSegmentRulesByGroupQuery(
    Guid SegmentGroupId
) : IRequest<Response<List<SegmentRuleDto>>>;

public sealed record SegmentRuleDto
{
    public Guid Id { get; set; }
    public string TraitKey { get; set; } = default!;
    public string Operator { get; set; } = default!;
    public string? Value { get; set; }
    public SegmentValueType ValueType { get; set; }
    public int SortOrder { get; set; }
}

public sealed class GetSegmentRulesByGroupHandler(
    SwitchlyDbContext context,
    IUserContext userContext
) : IRequestHandler<GetSegmentRulesByGroupQuery, Response<List<SegmentRuleDto>>>
{
    public async Task<Response<List<SegmentRuleDto>>> Handle(GetSegmentRulesByGroupQuery request, CancellationToken ct)
    {
        // Load segment group and its organization
        var segmentGroup = await context.SegmentGroups
            .AsNoTracking()
            .Where(sg => sg.Id == request.SegmentGroupId)
            .Select(sg => new { sg.Id, sg.OrganizationId })
            .FirstOrDefaultAsync(ct);

        if (segmentGroup is null)
            return Response<List<SegmentRuleDto>>.Fail("Segment group bulunamadı.");

        // AuthZ: user must be a member of the organization
        var isMember = await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m => m.OrganizationId == segmentGroup.OrganizationId && m.UserId == userContext.UserId, ct);

        if (!isMember)
            return Response<List<SegmentRuleDto>>.Fail("Bu segment group için yetkin yok.");

        var rules = await context.SegmentRules
            .AsNoTracking()
            .Where(r => r.SegmentGroupId == request.SegmentGroupId)
            .OrderBy(r => r.SortOrder)
            .Select(r => new SegmentRuleDto
            {
                Id = r.Id,
                TraitKey = r.TraitKey!,
                Operator = r.Operator!,
                Value = r.Value,
                ValueType = r.ValueType!.Value,
                SortOrder = r.SortOrder
            })
            .ToListAsync(ct);

        return Response<List<SegmentRuleDto>>.Ok(rules, "Segment rules");
    }
}