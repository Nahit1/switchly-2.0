using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.ProjectEnviroments.SetEnvironmentVariantWeights;

public sealed record SetEnvironmentVariantWeightsCommand(
    Guid FeatureFlagEnvironmentId,
    List<VariantWeightInput> Weights
) : IRequest<Response<SetEnvironmentVariantWeightsDto>>;

public sealed record VariantWeightInput(Guid VariantId, int Weight);

public sealed record SetEnvironmentVariantWeightsDto(
    Guid FeatureFlagEnvironmentId,
    List<VariantWeightDto> Weights
);

public sealed record VariantWeightDto(Guid VariantId, string VariantKey, int Weight);

public sealed class SetEnvironmentVariantWeightsHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<SetEnvironmentVariantWeightsCommand, Response<SetEnvironmentVariantWeightsDto>>
{
    public async Task<Response<SetEnvironmentVariantWeightsDto>> Handle(
        SetEnvironmentVariantWeightsCommand request, CancellationToken ct)
    {
        // Env → flag → project → organization zinciri ile yetki kontrolü yap.
        var env = await context.FeatureFlagEnvironments
            .Include(fe => fe.FeatureFlag)
                .ThenInclude(f => f.Variants)
            .Include(fe => fe.FeatureFlag)
                .ThenInclude(f => f.Project)
            .FirstOrDefaultAsync(fe => fe.Id == request.FeatureFlagEnvironmentId, ct);

        if (env is null)
            return Response<SetEnvironmentVariantWeightsDto>.Fail("Feature flag environment bulunamadı.");

        var isMember = await context.OrganizationMembers
            .AnyAsync(m =>
                m.OrganizationId == env.FeatureFlag.Project.OrganizationId &&
                m.UserId == userContext.UserId, ct);

        if (!isMember)
            throw new UnauthorizedAccessException("Bu organization için yetkin yok.");

        if (env.FeatureFlag.Type != FeatureFlagType.Multivariant)
            return Response<SetEnvironmentVariantWeightsDto>.Fail(
                "Sadece Multivariant flag'lere variant weight set edilebilir.");

        var validation = ValidateWeights(request.Weights, env.FeatureFlag.Variants);
        if (!validation.Success)
            return Response<SetEnvironmentVariantWeightsDto>.Fail(validation.Message!);

        // Replace-all: mevcut weight'leri sil, yenilerini ekle.
        var existing = await context.FeatureFlagEnvironmentVariantWeights
            .Where(w => w.FeatureFlagEnvironmentId == env.Id)
            .ToListAsync(ct);

        if (existing.Count > 0)
            context.FeatureFlagEnvironmentVariantWeights.RemoveRange(existing);

        var fresh = request.Weights.Select(w => new FeatureFlagEnvironmentVariantWeight
        {
            Id = Guid.NewGuid(),
            FeatureFlagEnvironmentId = env.Id,
            VariantId = w.VariantId,
            Weight = w.Weight
        }).ToList();

        context.FeatureFlagEnvironmentVariantWeights.AddRange(fresh);

        await context.SaveChangesAsync(ct);

        var variantsByKey = env.FeatureFlag.Variants.ToDictionary(v => v.Id, v => v.Key);
        var responseWeights = fresh
            .Select(w => new VariantWeightDto(w.VariantId, variantsByKey[w.VariantId], w.Weight))
            .ToList();

        return Response<SetEnvironmentVariantWeightsDto>.Ok(new SetEnvironmentVariantWeightsDto(
            env.Id, responseWeights));
    }

    private static Response<object> ValidateWeights(List<VariantWeightInput> weights, ICollection<Variant> flagVariants)
    {
        if (weights is null || weights.Count == 0)
            return Response<object>.Fail("Weight listesi boş olamaz.");

        if (weights.Any(w => w.Weight < 0 || w.Weight > 100))
            return Response<object>.Fail("Weight 0–100 arasında olmalı.");

        if (weights.Select(w => w.VariantId).Distinct().Count() != weights.Count)
            return Response<object>.Fail("Aynı variant birden fazla weight'e sahip olamaz.");

        var flagVariantIds = flagVariants.Select(v => v.Id).ToHashSet();
        if (weights.Any(w => !flagVariantIds.Contains(w.VariantId)))
            return Response<object>.Fail("Bir veya daha fazla variant bu flag'e ait değil.");

        if (weights.Sum(w => w.Weight) != 100)
            return Response<object>.Fail("Weight toplamı 100 olmalı.");

        return Response<object>.Ok(new object());
    }
}
