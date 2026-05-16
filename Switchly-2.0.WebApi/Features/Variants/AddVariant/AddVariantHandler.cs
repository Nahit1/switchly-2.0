using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.Variants.AddVariant;

public sealed record AddVariantCommand(
    Guid FeatureFlagId,
    string Key,
    string? Name,
    string? PayloadJson
) : IRequest<Response<AddVariantDto>>;

public sealed record AddVariantDto(
    Guid Id,
    string Key,
    string? Name,
    string? PayloadJson,
    int SortOrder
);

public sealed class AddVariantHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<AddVariantCommand, Response<AddVariantDto>>
{
    public async Task<Response<AddVariantDto>> Handle(AddVariantCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
            return Response<AddVariantDto>.Fail("Variant key boş olamaz.");

        var flag = await context.FeatureFlags
            .Include(f => f.Project)
            .Include(f => f.Variants)
            .FirstOrDefaultAsync(f => f.Id == request.FeatureFlagId, ct);

        if (flag is null)
            return Response<AddVariantDto>.Fail("Feature flag bulunamadı.");

        var isMember = await context.OrganizationMembers
            .AnyAsync(m =>
                m.OrganizationId == flag.Project.OrganizationId &&
                m.UserId == userContext.UserId, ct);

        if (!isMember)
            throw new UnauthorizedAccessException("Bu organization için yetkin yok.");

        if (flag.Type != FeatureFlagType.Multivariant)
            return Response<AddVariantDto>.Fail("Sadece Multivariant flag'lere variant eklenebilir.");

        var newKey = request.Key.Trim();
        if (flag.Variants.Any(v => string.Equals(v.Key, newKey, StringComparison.OrdinalIgnoreCase)))
            return Response<AddVariantDto>.Fail("Bu key ile zaten bir variant var.");

        var nextSortOrder = flag.Variants.Count == 0
            ? 0
            : flag.Variants.Max(v => v.SortOrder) + 1;

        var variant = new Variant
        {
            Id = Guid.NewGuid(),
            FeatureFlagId = flag.Id,
            Key = newKey,
            Name = request.Name?.Trim(),
            PayloadJson = request.PayloadJson,
            SortOrder = nextSortOrder,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Variants.Add(variant);
        await context.SaveChangesAsync(ct);

        return Response<AddVariantDto>.Ok(new AddVariantDto(
            variant.Id, variant.Key, variant.Name, variant.PayloadJson, variant.SortOrder));
    }
}
