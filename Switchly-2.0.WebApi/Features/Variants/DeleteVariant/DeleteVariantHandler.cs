using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;

namespace Switchly_2._0.WebApi.Features.Variants.DeleteVariant;

public sealed record DeleteVariantCommand(Guid VariantId) : IRequest<Response<bool>>;

// Variant silinince DB seviyesinde FK cascade ile ilgili EnvironmentVariantWeight ve
// SegmentTargetingVariantWeight kayıtları da düşer. Bu durumda env/targeting weight
// toplamı 100'ün altına inebilir — evaluator missing variant'ı 0 weight gibi davranır,
// kullanıcı kalan variant'lar için weight'leri yeniden set etmek zorunda.
public sealed class DeleteVariantHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<DeleteVariantCommand, Response<bool>>
{
    public async Task<Response<bool>> Handle(DeleteVariantCommand request, CancellationToken ct)
    {
        var variant = await context.Variants
            .Include(v => v.FeatureFlag)
                .ThenInclude(f => f.Project)
            .Include(v => v.FeatureFlag)
                .ThenInclude(f => f.Variants)
            .FirstOrDefaultAsync(v => v.Id == request.VariantId, ct);

        if (variant is null)
            return Response<bool>.Fail("Variant bulunamadı.");

        var isMember = await context.OrganizationMembers
            .AnyAsync(m =>
                m.OrganizationId == variant.FeatureFlag.Project.OrganizationId &&
                m.UserId == userContext.UserId, ct);

        if (!isMember)
            throw new UnauthorizedAccessException("Bu organization için yetkin yok.");

        // Multivariant invariant'ı: en az 2 variant kalmalı.
        if (variant.FeatureFlag.Variants.Count <= 2)
            return Response<bool>.Fail("Multivariant flag en az 2 variant taşımalı, son ikisi silinemez.");

        context.Variants.Remove(variant);
        await context.SaveChangesAsync(ct);

        return Response<bool>.Ok(true);
    }
}
