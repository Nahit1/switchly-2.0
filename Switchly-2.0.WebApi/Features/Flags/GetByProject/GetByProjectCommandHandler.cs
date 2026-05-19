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
    public int SortOrder { get; set; }
}

public sealed record VariantWeightDto
{
    public Guid VariantId { get; set; }
    public int Weight { get; set; }
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

    public List<VariantWeightDto> VariantWeights { get; set; } = new();
    public List<SegmentGroupsDto> SegmentGroups { get; set; } = new();

    // Aktif veya pause edilmiş rollout schedule (terminal state'tekiler dahil edilmez).
    // Null = bu env için aktif rollout yok → UI "Schedule Oluştur" butonu gösterir.
    public EnvRolloutScheduleDto? RolloutSchedule { get; set; }
}

public sealed record EnvRolloutScheduleDto
{
    public Guid Id { get; set; }
    public Guid? TargetVariantId { get; set; }
    public string? TargetVariantKey { get; set; }
    public string Status { get; set; } = default!;
    public int CurrentStepIndex { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? LastTransitionAt { get; set; }
    public DateTimeOffset? PausedAt { get; set; }
    public List<EnvRolloutScheduleStepDto> Steps { get; set; } = new();

    // Guardrail (Seviye 2) config + state.
    public int? ErrorThreshold { get; set; }
    public int ErrorWindowMinutes { get; set; }
    public string? MinSeverity { get; set; }
    public string? RolledBackReason { get; set; }
}

public sealed record EnvRolloutScheduleStepDto
{
    public int StepIndex { get; set; }
    public int Percentage { get; set; }
    public int DurationMinutes { get; set; }
    public DateTimeOffset? PromotedAt { get; set; }
}

public sealed record SegmentGroupsDto
{
    public Guid Id { get; set; }                                  // FeatureFlagSegmentTargeting.Id (update için)
    public string Name { get; set; } = default!;
    public string Key { get; set; } = default!;
    public string? Description { get; set; }
    public LogicalOperator LogicalOperator { get; set; }          // segment grup içi rule birleşim mantığı
    public RolloutKind RolloutKind { get; set; }                  // targeting'in kendi rollout'u
    public int RolloutPercentage { get; set; }
    public int Priority { get; set; }
    public bool IsEnabled { get; set; }
    public List<VariantWeightDto> VariantWeights { get; set; } = new();
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
                        VariantWeights = fe.VariantWeights
                            .Select(w => new VariantWeightDto
                            {
                                VariantId = w.VariantId,
                                Weight = w.Weight
                            }).ToList(),
                        SegmentGroups = fe.SegmentTargetings
                            .OrderByDescending(x => x.Priority)
                            .Select(x=>new SegmentGroupsDto
                            {
                                Id = x.Id,
                                Name = x.SegmentGroup.Name,
                                Key = x.SegmentGroup.Key,
                                Description = x.SegmentGroup.Description,
                                LogicalOperator = x.SegmentGroup.LogicalOperator,
                                RolloutKind = x.RolloutKind,
                                RolloutPercentage = x.RolloutPercentage,
                                Priority = x.Priority,
                                IsEnabled = x.IsEnabled,
                                VariantWeights = x.VariantWeights
                                    .Select(w => new VariantWeightDto
                                    {
                                        VariantId = w.VariantId,
                                        Weight = w.Weight
                                    }).ToList(),
                                SegmentRules = x.SegmentGroup.Rules
                                    .Select(r => new SegmentRuleDto
                                    {
                                        TraitKey = r.TraitKey,
                                        Operator = r.Operator,
                                        Value = r.Value,
                                    }).ToList()
                            }).ToList(),

                        // En güncel schedule (Active/Paused öncelikli; aksi takdirde son 24h
                        // içindeki terminal — auto-rollback bildirimi için UI'da kısa süre görünür).
                        RolloutSchedule = context.RolloutSchedules
                            .Where(s => s.FeatureFlagEnvironmentId == fe.Id
                                        && (s.Status == RolloutScheduleStatus.Active
                                            || s.Status == RolloutScheduleStatus.Paused
                                            || s.CreatedAt >= DateTimeOffset.UtcNow.AddDays(-1)))
                            .OrderByDescending(s =>
                                s.Status == RolloutScheduleStatus.Active ||
                                s.Status == RolloutScheduleStatus.Paused ? 1 : 0)
                            .ThenByDescending(s => s.CreatedAt)
                            .Select(s => new EnvRolloutScheduleDto
                            {
                                Id = s.Id,
                                TargetVariantId = s.TargetVariantId,
                                TargetVariantKey = s.TargetVariant != null ? s.TargetVariant.Key : null,
                                Status = s.Status.ToString(),
                                CurrentStepIndex = s.CurrentStepIndex,
                                StartedAt = s.StartedAt,
                                LastTransitionAt = s.LastTransitionAt,
                                PausedAt = s.PausedAt,
                                Steps = s.Steps
                                    .OrderBy(st => st.StepIndex)
                                    .Select(st => new EnvRolloutScheduleStepDto
                                    {
                                        StepIndex = st.StepIndex,
                                        Percentage = st.Percentage,
                                        DurationMinutes = st.DurationMinutes,
                                        PromotedAt = st.PromotedAt
                                    }).ToList(),
                                ErrorThreshold = s.ErrorThreshold,
                                ErrorWindowMinutes = s.ErrorWindowMinutes,
                                MinSeverity = s.MinSeverity.ToString(),
                                RolledBackReason = s.RolledBackReason
                            })
                            .FirstOrDefault()

                    }).ToList(),
                Variants = f.Variants
                    .OrderBy(v => v.SortOrder)
                    .Select(v => new GetFlagVariantDto
                    {
                        Id = v.Id,
                        Key = v.Key,
                        Name = v.Name,
                        PayloadJson = v.PayloadJson,
                        SortOrder = v.SortOrder
                    })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        return Response<List<GetFlagByProjectDto>>.Ok(flagList, "Flag list");
    }
}