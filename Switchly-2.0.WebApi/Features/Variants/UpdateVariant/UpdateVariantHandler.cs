using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;

namespace Switchly_2._0.WebApi.Features.Variants.UpdateVariant;

public sealed record UpdateVariantCommand(
    Guid VariantId,
    string? Name,
    string? PayloadJson
) : IRequest<Response<UpdateVariantDto>>;

public sealed record UpdateVariantDto(
    Guid Id,
    string Key,
    string? Name,
    string? PayloadJson,
    int SortOrder
);

// Variant Key bilinçli olarak güncellenmiyor: analytics/log tarafında variant key
// stable identifier olarak kullanılır; runtime'da değiştirmek mevcut verileri kirletir.
// Rename istenirse delete + add önerilir.
public sealed class UpdateVariantHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<UpdateVariantCommand, Response<UpdateVariantDto>>
{
    public async Task<Response<UpdateVariantDto>> Handle(UpdateVariantCommand request, CancellationToken ct)
    {
        var variant = await context.Variants
            .Include(v => v.FeatureFlag)
                .ThenInclude(f => f.Project)
            .FirstOrDefaultAsync(v => v.Id == request.VariantId, ct);

        if (variant is null)
            return Response<UpdateVariantDto>.Fail("Variant bulunamadı.");

        var isMember = await context.OrganizationMembers
            .AnyAsync(m =>
                m.OrganizationId == variant.FeatureFlag.Project.OrganizationId &&
                m.UserId == userContext.UserId, ct);

        if (!isMember)
            throw new UnauthorizedAccessException("Bu organization için yetkin yok.");

        variant.Name = request.Name?.Trim();
        variant.PayloadJson = request.PayloadJson;

        await context.SaveChangesAsync(ct);

        return Response<UpdateVariantDto>.Ok(new UpdateVariantDto(
            variant.Id, variant.Key, variant.Name, variant.PayloadJson, variant.SortOrder));
    }
}
