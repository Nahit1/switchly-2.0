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
    FeatureFlagType Type // Boolean, Multivariant, Config
): IRequest<Response<CreateFlagDto>>;

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
        
        await context.SaveChangesAsync(cancellationToken);
        
        return Response<CreateFlagDto>.Ok(new CreateFlagDto
        {
            Id = flag.Id,
        });
    }
}