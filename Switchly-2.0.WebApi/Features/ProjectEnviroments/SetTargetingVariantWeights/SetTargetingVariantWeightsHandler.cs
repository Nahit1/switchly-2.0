using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.ProjectEnviroments.SetTargetingVariantWeights;

public sealed record SetTargetingVariantWeightsCommand(
    Guid FeatureFlagSegmentTargetingId,
    List<TargetingVariantWeightInput> Weights
) : IRequest<Response<SetTargetingVariantWeightsDto>>;

public sealed record TargetingVariantWeightInput(Guid VariantId, int Weight);

public sealed record SetTargetingVariantWeightsDto(
    Guid FeatureFlagSegmentTargetingId,
    List<TargetingVariantWeightDto> Weights
);

public sealed record TargetingVariantWeightDto(Guid VariantId, string VariantKey, int Weight);

public sealed class SetTargetingVariantWeightsHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<SetTargetingVariantWeightsCommand, Response<SetTargetingVariantWeightsDto>>
{
    public async Task<Response<SetTargetingVariantWeightsDto>> Handle(
        SetTargetingVariantWeightsCommand request, CancellationToken ct)
    {
        // Targeting → env → flag → project → organization zinciri ile yetki kontrolü.
        var targeting = await context.FeatureFlagSegmentTargetings
            .Include(t => t.FeatureFlagEnvironment)
                .ThenInclude(fe => fe.FeatureFlag)
                    .ThenInclude(f => f.Variants)
            .Include(t => t.FeatureFlagEnvironment)
                .ThenInclude(fe => fe.FeatureFlag)
                    .ThenInclude(f => f.Project)
            .FirstOrDefaultAsync(t => t.Id == request.FeatureFlagSegmentTargetingId, ct);

        if (targeting is null)
            return Response<SetTargetingVariantWeightsDto>.Fail("Targeting bulunamadı.");

        var flag = targeting.FeatureFlagEnvironment.FeatureFlag;

        var isMember = await context.OrganizationMembers
            .AnyAsync(m =>
                m.OrganizationId == flag.Project.OrganizationId &&
                m.UserId == userContext.UserId, ct);

        if (!isMember)
            throw new UnauthorizedAccessException("Bu organization için yetkin yok.");

        if (flag.Type != FeatureFlagType.Multivariant)
            return Response<SetTargetingVariantWeightsDto>.Fail(
                "Sadece Multivariant flag'lere variant weight set edilebilir.");

        var validation = ValidateWeights(request.Weights, flag.Variants);
        if (!validation.Success)
            return Response<SetTargetingVariantWeightsDto>.Fail(validation.Message!);

        var existing = await context.FeatureFlagSegmentTargetingVariantWeights
            .Where(w => w.FeatureFlagSegmentTargetingId == targeting.Id)
            .ToListAsync(ct);

        if (existing.Count > 0)
            context.FeatureFlagSegmentTargetingVariantWeights.RemoveRange(existing);

        var fresh = request.Weights.Select(w => new FeatureFlagSegmentTargetingVariantWeight
        {
            Id = Guid.NewGuid(),
            FeatureFlagSegmentTargetingId = targeting.Id,
            VariantId = w.VariantId,
            Weight = w.Weight
        }).ToList();

        context.FeatureFlagSegmentTargetingVariantWeights.AddRange(fresh);

        await context.SaveChangesAsync(ct);

        var variantsByKey = flag.Variants.ToDictionary(v => v.Id, v => v.Key);
        var responseWeights = fresh
            .Select(w => new TargetingVariantWeightDto(w.VariantId, variantsByKey[w.VariantId], w.Weight))
            .ToList();

        return Response<SetTargetingVariantWeightsDto>.Ok(new SetTargetingVariantWeightsDto(
            targeting.Id, responseWeights));
    }

    private static Response<object> ValidateWeights(List<TargetingVariantWeightInput> weights, ICollection<Variant> flagVariants)
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
