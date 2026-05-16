using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.Flags.CreateFlag;

public record CreateFlagCommandHandler(
    Guid OrganizationId,
    Guid ProjectId,
    string Key,
    string Name,
    string? Description,
    FeatureFlagType Type, // Boolean, Multivariant, Config
    List<VariantInput>? Variants
): IRequest<Response<CreateFlagDto>>;

public sealed record VariantInput(
    string Key,
    string? Name,
    string? PayloadJson
);

public sealed record CreateFlagDto
{
    public Guid Id { get; init; }
}


public class CreateOrganizationHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<CreateFlagCommandHandler, Response<CreateFlagDto>>
{
    public async Task<Response<CreateFlagDto>> Handle(CreateFlagCommandHandler request, CancellationToken cancellationToken)
    {
        var isMember = await context.OrganizationMembers
            .AnyAsync(m =>
                m.OrganizationId == request.OrganizationId &&
                m.UserId == userContext.UserId, cancellationToken);

        if (!isMember)
            throw new UnauthorizedAccessException("Bu organization için yetkin yok.");

        var project = await context.Projects
            .FirstOrDefaultAsync(p =>
                p.Id == request.ProjectId &&
                p.OrganizationId == request.OrganizationId, cancellationToken);

        if (project is null)
            throw new InvalidOperationException("Project bulunamadı.");

        var exists = await context.FeatureFlags
            .AnyAsync(f =>
                f.ProjectId == project.Id &&
                f.Key == request.Key, cancellationToken);

        if (exists)
            throw new InvalidOperationException("Bu key ile daha önce flag tanımlanmış.");

        // Variant input validation — Type'a göre değişir.
        var variantsValidation = ValidateVariants(request.Type, request.Variants);
        if (!variantsValidation.Success)
            return Response<CreateFlagDto>.Fail(variantsValidation.Message!);

        var envs = await context.ProjectEnvironments
            .AsNoTracking()
            .Where(e => e.ProjectId == request.ProjectId)
            .OrderBy(e => e.SortOrder)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;

        var flag = new FeatureFlag
        {
            ProjectId = project.Id,
            Key = request.Key,
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            IsArchived = false,
            CreatedAt = now
        };

        context.FeatureFlags.Add(flag);

        var variants = (request.Variants ?? [])
            .Select((v, i) => new Variant
            {
                Id = Guid.NewGuid(),
                FeatureFlagId = flag.Id,
                Key = v.Key.Trim(),
                Name = v.Name?.Trim(),
                PayloadJson = v.PayloadJson,
                SortOrder = i,
                CreatedAt = now
            })
            .ToList();

        if (variants.Count > 0)
            context.Variants.AddRange(variants);

        var flagEnvs = envs.Select(env =>
        {
            var isDefault = env.IsDefault;

            return new FeatureFlagEnvironment
            {
                Id = Guid.NewGuid(),
                FeatureFlagId = flag.Id,
                ProjectEnvironmentId = env.Id,

                IsEnabled = isDefault,
                DefaultRolloutKind = isDefault ? RolloutKind.AllUsers : RolloutKind.Off,
                DefaultRolloutPercentage = isDefault ? 100 : 0,

                UpdatedAt = now
            };
        }).ToList();

        context.FeatureFlagEnvironments.AddRange(flagEnvs);

        // Multivariant'ta default env'e baseline weight: ilk variant=100, diğerleri=0.
        // Boolean'ın "default env AllUsers/100" pattern'inin Multivariant karşılığı.
        if (request.Type == FeatureFlagType.Multivariant && variants.Count > 0)
        {
            var defaultEnv = flagEnvs.FirstOrDefault(fe =>
                envs.First(e => e.Id == fe.ProjectEnvironmentId).IsDefault);

            if (defaultEnv is not null)
            {
                var defaultWeights = variants.Select((v, i) => new FeatureFlagEnvironmentVariantWeight
                {
                    Id = Guid.NewGuid(),
                    FeatureFlagEnvironmentId = defaultEnv.Id,
                    VariantId = v.Id,
                    Weight = i == 0 ? 100 : 0
                }).ToList();

                context.FeatureFlagEnvironmentVariantWeights.AddRange(defaultWeights);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return Response<CreateFlagDto>.Ok(new CreateFlagDto
        {
            Id = flag.Id,
        });
    }

    private static Response<object> ValidateVariants(FeatureFlagType type, List<VariantInput>? variants)
    {
        var hasVariants = variants is { Count: > 0 };

        if (type == FeatureFlagType.Boolean)
        {
            if (hasVariants)
                return Response<object>.Fail("Boolean flag için Variants gönderilemez.");
            return Response<object>.Ok(new object());
        }

        if (type == FeatureFlagType.Multivariant)
        {
            if (!hasVariants || variants!.Count < 2)
                return Response<object>.Fail("Multivariant flag için en az 2 variant zorunludur.");

            if (variants.Any(v => string.IsNullOrWhiteSpace(v.Key)))
                return Response<object>.Fail("Variant key boş olamaz.");

            var keys = variants.Select(v => v.Key.Trim()).ToList();
            if (keys.Count != keys.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                return Response<object>.Fail("Variant key'leri benzersiz olmalı.");
        }

        // Config: MVP'de variant şartı yok; ileride payload zorunluluğu eklenebilir.
        return Response<object>.Ok(new object());
    }
}