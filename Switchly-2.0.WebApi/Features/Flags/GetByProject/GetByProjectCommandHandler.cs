using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;
using UnauthorizedAccessException = System.UnauthorizedAccessException;

namespace Switchly_2._0.WebApi.Features.Flags.GetByProject;


public sealed record GetOrganizationListQuery(Guid organizationId, Guid projectId)
    : IRequest<Response<List<GetFlagByProjectDto>>>;

public sealed record GetFlagByProjectDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public FeatureFlagType Type { get; set; }   // Boolean, Multivariant, Config
    public DateTimeOffset CreatedAt { get; set; }

    public List<GetFlagEnvironmentDto> Environments { get; set; } = new();
    public List<GetFlagVariantDto> Variants { get; set; } = new();
    
    
}

public sealed record GetFlagVariantDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = default!;
    public string? Name { get; set; }
    public string? PayloadJson { get; set; }
}
public sealed record GetFlagEnvironmentDto
{
    public Guid ProjectEnvironmentId { get; set; }
    public Guid FeatureFlagEnvironmentId { get; set; }
    public string EnvironmentKey { get; set; } = default!;
    public string EnvironmentName { get; set; } = default!;

    public bool IsEnabled { get; set; }
    public RolloutKind DefaultRolloutKind { get; set; }
    public int DefaultRolloutPercentage { get; set; }
    
    public List<SegmentGroupsDto> SegmentGroups { get; set; } = new();
}

public sealed record SegmentGroupsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Key { get; set; } = default!;
    public string? Description { get; set; }
    public ICollection<SegmentRuleDto> SegmentRules { get; set; }
}

public sealed record SegmentRuleDto
{
    public string? TraitKey { get; set; }
    public string? Operator { get; set; }
    public string? Value { get; set; }
}

public class GetByProjectCommandHandler(SwitchlyDbContext context, IUserContext userContext)
    :IRequestHandler<GetOrganizationListQuery, Response<List<GetFlagByProjectDto>>>
{
    public async Task<Response<List<GetFlagByProjectDto>>> Handle(GetOrganizationListQuery request, CancellationToken cancellationToken)
    {
        var isMember = await context.OrganizationMembers
            .AnyAsync(m =>
                m.OrganizationId == request.organizationId &&
                m.UserId == userContext.UserId, cancellationToken);

        if (!isMember)
            throw new UnauthorizedAccessException("Bu organization için yetkin yok.");
        
        var flagList = await context.FeatureFlags
            .AsNoTracking()
            .Where(f => f.ProjectId == request.projectId)
            .OrderBy(f => f.Key)
            .Select(f => new GetFlagByProjectDto
            {
                Id = f.Id,
                Key = f.Key,
                Name = f.Name,
                Description = f.Description,
                Type = f.Type,
                CreatedAt = f.CreatedAt,
                Environments = f.Environments
                    .OrderBy(fe => fe.ProjectEnvironment.SortOrder)
                    .Select(fe => new GetFlagEnvironmentDto
                    {
                        ProjectEnvironmentId = fe.ProjectEnvironmentId,
                        EnvironmentKey = fe.ProjectEnvironment.Key,
                        EnvironmentName = fe.ProjectEnvironment.Name,
                        IsEnabled = fe.IsEnabled,
                        DefaultRolloutKind = fe.DefaultRolloutKind,
                        DefaultRolloutPercentage = fe.DefaultRolloutPercentage,
                        FeatureFlagEnvironmentId = fe.Id,
                        SegmentGroups = fe.SegmentTargetings
                            .Select(x=>new SegmentGroupsDto
                            {
                                Id = x.Id,
                                Name = x.SegmentGroup.Name,
                                Key = x.SegmentGroup.Key,
                                Description = x.SegmentGroup.Description,
                                SegmentRules = x.SegmentGroup.Rules
                                    .Select(r => new SegmentRuleDto
                                    {
                                        TraitKey = r.TraitKey,
                                        Operator = r.Operator,
                                        Value = r.Value,
                                    }).ToList()
                            }).ToList()
                        
                    }).ToList(),
                Variants = f.Variants
                    .OrderBy(v => v.Key)
                    .Select(v => new GetFlagVariantDto
                    {
                        Id = v.Id,
                        Key = v.Key,
                        Name = v.Name,
                        PayloadJson = v.PayloadJson
                    })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        return Response<List<GetFlagByProjectDto>>.Ok(flagList, "Flag list");
    }
}