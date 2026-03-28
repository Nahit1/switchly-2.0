using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.SegmentRules.CreateSegmentRule;

public sealed record CreateSegmentRuleCommand(
    Guid SegmentGroupId,
    string TraitKey,
    string Operator,
    string? Value,
    SegmentValueType ValueType,
    int SortOrder
) : IRequest<Response<Guid>>;

public sealed class CreateSegmentRuleHandler(
    SwitchlyDbContext context,
    IUserContext userContext
) : IRequestHandler<CreateSegmentRuleCommand, Response<Guid>>
{
    public async Task<Response<Guid>> Handle(CreateSegmentRuleCommand request, CancellationToken ct)
    {
        // SegmentGroup must exist and belong to an organization the user is member of
        var segmentGroup = await context.SegmentGroups
            .AsNoTracking()
            .Where(sg => sg.Id == request.SegmentGroupId)
            .Select(sg => new
            {
                sg.Id,
                sg.OrganizationId
            })
            .FirstOrDefaultAsync(ct);

        if (segmentGroup is null)
            return Response<Guid>.Fail("Segment group bulunamadı.");

        // AuthZ: user must be member of the organization owning the segment group
        var isMember = await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m =>
                m.OrganizationId == segmentGroup.OrganizationId &&
                m.UserId == userContext.UserId,
                ct);

        if (!isMember)
            return Response<Guid>.Fail("Bu segment group için yetkin yok.");

        // MVP validations (flat condition only)
        if (string.IsNullOrWhiteSpace(request.TraitKey))
            return Response<Guid>.Fail("TraitKey zorunludur.");

        if (string.IsNullOrWhiteSpace(request.Operator))
            return Response<Guid>.Fail("Operator zorunludur.");

        var entity = new SegmentRule
        {
            Id = Guid.NewGuid(),
            SegmentGroupId = request.SegmentGroupId,

            // MVP: flat rule
            ParentRuleId = null,
            NodeType = SegmentNodeType.Condition,
            LogicalOperator = null,

            TraitKey = request.TraitKey.Trim(),
            Operator = request.Operator.Trim(),
            Value = request.Value,
            ValueType = request.ValueType,
            SortOrder = request.SortOrder
        };

        context.SegmentRules.Add(entity);
        await context.SaveChangesAsync(ct);

        return Response<Guid>.Ok(entity.Id, "Segment rule created");
    }
}