using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;

namespace Switchly_2._0.WebApi.Features.SegmetGroups.RemoveSegmentGroup;

public sealed record RemoveSegmentGroupCommand(Guid SegmentGroupId)
    : IRequest<Response<string>>;

public sealed class RemoveSegmentGroupHandler(
    SwitchlyDbContext context,
    IUserContext userContext
) : IRequestHandler<RemoveSegmentGroupCommand, Response<string>>
{
    public async Task<Response<string>> Handle(RemoveSegmentGroupCommand request, CancellationToken ct)
    {
        var group = await context.SegmentGroups
            .FirstOrDefaultAsync(sg => sg.Id == request.SegmentGroupId, ct);

        if (group is null)
            return Response<string>.Fail("Segment group bulunamadı.");

        var isMember = await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m => m.OrganizationId == group.OrganizationId && m.UserId == userContext.UserId, ct);

        if (!isMember)
            return Response<string>.Fail("Bu işlem için yetkin yok.");

        var inUse = await context.FeatureFlagSegmentTargetings
            .AsNoTracking()
            .AnyAsync(t => t.SegmentGroupId == request.SegmentGroupId, ct);

        if (inUse)
            return Response<string>.Fail("Bu segment bir veya daha fazla flag tarafından kullanılıyor. Önce flag'lerden kaldırın.");

        context.SegmentGroups.Remove(group);
        await context.SaveChangesAsync(ct);

        return Response<string>.Ok("Segment group kaldırıldı.");
    }
}
