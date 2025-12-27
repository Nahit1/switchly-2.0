using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;

namespace Switchly_2._0.WebApi.Features.Variants.GetVariantsByFlag;

public sealed record GetVariantsByFlagQuery(
    Guid OrganizationId,
    Guid ProjectId,
    Guid FeatureFlagId
) : IRequest<Response<List<VariantDto>>>;

public sealed record VariantDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = default!;
    public string? Name { get; set; }
    public string? PayloadJson { get; set; }
}

public class GetVariantsByFlagHandler(
    SwitchlyDbContext context,
    IUserContext userContext
) : IRequestHandler<GetVariantsByFlagQuery, Response<List<VariantDto>>>
{
    public async Task<Response<List<VariantDto>>> Handle(GetVariantsByFlagQuery request, CancellationToken cancellationToken)
    {
        var isMember = await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m => m.OrganizationId == request.OrganizationId && m.UserId == userContext.UserId, cancellationToken);

        if (!isMember)
            return Response<List<VariantDto>>.Fail("Bu organization için yetkin yok.");

        var flagOk = await context.FeatureFlags
            .AsNoTracking()
            .AnyAsync(f => f.Id == request.FeatureFlagId && f.ProjectId == request.ProjectId, cancellationToken);

        if (!flagOk)
            return Response<List<VariantDto>>.Fail("Feature flag bulunamadı.");

        var list = await context.Variants
            .AsNoTracking()
            .Where(v => v.FeatureFlagId == request.FeatureFlagId)
            .OrderBy(v => v.Key)
            .Select(v => new VariantDto
            {
                Id = v.Id,
                Key = v.Key,
                Name = v.Name,
                PayloadJson = v.PayloadJson
            })
            .ToListAsync(cancellationToken);

        return Response<List<VariantDto>>.Ok(list, "Variant list");
    }
}