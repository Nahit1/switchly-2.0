using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.Variants.CreateVariant;

public sealed record CreateVariantCommand(
    Guid OrganizationId,
    Guid ProjectId,
    Guid FeatureFlagId,
    string Key,
    string? Name,
    string? PayloadJson
) : IRequest<Response<Guid>>;

public class CreateVariantCommandHandler(
    SwitchlyDbContext context,
    IUserContext userContext
) : IRequestHandler<CreateVariantCommand, Response<Guid>>
{
    public async Task<Response<Guid>> Handle(CreateVariantCommand request, CancellationToken cancellationToken)
    {
        var isMember = await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m => m.OrganizationId == request.OrganizationId && m.UserId == userContext.UserId, cancellationToken);

        if (!isMember)
            return Response<Guid>.Fail("Bu organization için yetkin yok.");

        var flag = await context.FeatureFlags
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == request.FeatureFlagId && f.ProjectId == request.ProjectId, cancellationToken);

        if (flag is null)
            return Response<Guid>.Fail("Feature flag bulunamadı.");

        if (flag.Type == FeatureFlagType.Boolean)
            return Response<Guid>.Fail("Boolean flag için variant eklenemez.");

        var key = request.Key.Trim();

        var exists = await context.Variants
            .AsNoTracking()
            .AnyAsync(v => v.FeatureFlagId == request.FeatureFlagId && v.Key == key, cancellationToken);

        if (exists)
            return Response<Guid>.Fail("Bu key ile daha önce variant tanımlanmış.");

        if (flag.Type == FeatureFlagType.Config && string.IsNullOrWhiteSpace(request.PayloadJson))
            return Response<Guid>.Fail("Config flag için PayloadJson zorunludur.");

        var now = DateTimeOffset.UtcNow;

        var variant = new Variant
        {
            Id = Guid.NewGuid(),
            FeatureFlagId = request.FeatureFlagId,
            Key = key,
            Name = request.Name?.Trim(),
            PayloadJson = request.PayloadJson, // Multivariant için null olabilir
            CreatedAt = now
        };

        context.Variants.Add(variant);
        await context.SaveChangesAsync(cancellationToken);

        return Response<Guid>.Ok(variant.Id, "Variant created");
    }
}