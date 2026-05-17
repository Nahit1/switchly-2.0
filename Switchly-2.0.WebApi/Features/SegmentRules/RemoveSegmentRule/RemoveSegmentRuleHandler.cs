using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;

namespace Switchly_2._0.WebApi.Features.SegmentRules.RemoveSegmentRule;

public sealed record RemoveSegmentRuleCommand(Guid RuleId)
    : IRequest<Response<string>>;

public sealed class RemoveSegmentRuleHandler(
    SwitchlyDbContext context,
    IUserContext userContext
) : IRequestHandler<RemoveSegmentRuleCommand, Response<string>>
{
    public async Task<Response<string>> Handle(RemoveSegmentRuleCommand request, CancellationToken ct)
    {
        // Org boundary chain: rule → segmentGroup → org
        var rule = await context.SegmentRules
            .Include(r => r.SegmentGroup)
            .FirstOrDefaultAsync(r => r.Id == request.RuleId, ct);

        if (rule is null)
            return Response<string>.Fail("Segment rule bulunamadı.");

        var orgId = rule.SegmentGroup.OrganizationId;

        var isMember = await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m => m.OrganizationId == orgId && m.UserId == userContext.UserId, ct);

        if (!isMember)
            return Response<string>.Fail("Bu işlem için yetkin yok.");

        context.SegmentRules.Remove(rule);
        await context.SaveChangesAsync(ct);

        return Response<string>.Ok("Segment rule kaldırıldı.");
    }
}
