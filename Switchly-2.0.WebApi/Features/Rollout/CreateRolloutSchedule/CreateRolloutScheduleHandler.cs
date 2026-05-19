using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.Rollout.CreateRolloutSchedule;

public sealed record CreateRolloutScheduleCommand(
    Guid FeatureFlagEnvironmentId,
    Guid? TargetVariantId,
    List<RolloutStepInput> Steps,
    int? ErrorThreshold,
    int? ErrorWindowMinutes,
    string? MinSeverity
) : IRequest<Response<CreateRolloutScheduleDto>>;

public sealed record RolloutStepInput(int Percentage, int DurationMinutes);

public sealed record CreateRolloutScheduleDto(Guid ScheduleId);

public sealed class CreateRolloutScheduleHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<CreateRolloutScheduleCommand, Response<CreateRolloutScheduleDto>>
{
    public async Task<Response<CreateRolloutScheduleDto>> Handle(
        CreateRolloutScheduleCommand request, CancellationToken ct)
    {
        if (request.Steps is null || request.Steps.Count < 1)
            return Response<CreateRolloutScheduleDto>.Fail("En az 1 step gerekli.");

        if (request.Steps.Any(s => s.Percentage < 0 || s.Percentage > 100))
            return Response<CreateRolloutScheduleDto>.Fail("Step yüzdeleri 0-100 arasında olmalı.");

        // Step yüzdeleri monoton artmalı (çözmek istenebilir ama MVP'de zorunlu).
        for (var i = 1; i < request.Steps.Count; i++)
        {
            if (request.Steps[i].Percentage <= request.Steps[i - 1].Percentage)
                return Response<CreateRolloutScheduleDto>.Fail("Adımlar artan yüzdeyle sıralanmalı.");
        }

        // Env + Flag + variants + mevcut weights.
        var env = await context.FeatureFlagEnvironments
            .Include(e => e.FeatureFlag)
                .ThenInclude(f => f.Project)
            .Include(e => e.FeatureFlag)
                .ThenInclude(f => f.Variants)
            .Include(e => e.VariantWeights)
            .FirstOrDefaultAsync(e => e.Id == request.FeatureFlagEnvironmentId, ct);

        if (env is null)
            return Response<CreateRolloutScheduleDto>.Fail("Feature flag environment bulunamadı.");

        var isMember = await context.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == env.FeatureFlag.Project.OrganizationId
                           && m.UserId == userContext.UserId, ct);
        if (!isMember)
            throw new UnauthorizedAccessException("Bu organization için yetkin yok.");

        // Multivariant'sa TargetVariantId zorunlu + flag'in variant'ı olmalı.
        if (env.FeatureFlag.Type == FeatureFlagType.Multivariant)
        {
            if (!request.TargetVariantId.HasValue)
                return Response<CreateRolloutScheduleDto>.Fail("Multivariant flag için target variant zorunludur.");
            if (env.FeatureFlag.Variants.All(v => v.Id != request.TargetVariantId.Value))
                return Response<CreateRolloutScheduleDto>.Fail("Target variant bu flag'e ait değil.");
        }

        // Bu env'de aktif/pause schedule var mı? (filtered unique index var ama erken uyarı için query.)
        var hasActive = await context.RolloutSchedules
            .AnyAsync(s => s.FeatureFlagEnvironmentId == env.Id
                           && (s.Status == RolloutScheduleStatus.Active
                               || s.Status == RolloutScheduleStatus.Paused
                               || s.Status == RolloutScheduleStatus.Draft), ct);
        if (hasActive)
            return Response<CreateRolloutScheduleDto>.Fail("Bu env'de zaten aktif/duraklatılmış bir schedule var.");

        var now = DateTimeOffset.UtcNow;

        // Pre-schedule snapshot.
        var snapshot = RolloutWeightsApplier.TakeSnapshot(
            env, env.FeatureFlag.Type, env.VariantWeights.ToList());

        // Guardrail: ErrorThreshold > 0 ise auto-rollback aktif.
        var threshold = request.ErrorThreshold.HasValue && request.ErrorThreshold.Value > 0
            ? request.ErrorThreshold
            : null;
        var window = request.ErrorWindowMinutes.HasValue && request.ErrorWindowMinutes.Value > 0
            ? request.ErrorWindowMinutes.Value
            : 10;
        var minSeverity = Enum.TryParse<ErrorSeverity>(request.MinSeverity, ignoreCase: true, out var sev)
            ? sev
            : ErrorSeverity.Error;

        var schedule = new RolloutSchedule
        {
            Id = Guid.NewGuid(),
            FeatureFlagEnvironmentId = env.Id,
            TargetVariantId = env.FeatureFlag.Type == FeatureFlagType.Multivariant
                ? request.TargetVariantId
                : null,
            Status = RolloutScheduleStatus.Active,
            CurrentStepIndex = 0,
            StartedAt = now,
            LastTransitionAt = now,
            CreatedAt = now,
            PreScheduleSnapshot = snapshot,
            ErrorThreshold = threshold,
            ErrorWindowMinutes = window,
            MinSeverity = minSeverity
        };

        for (var i = 0; i < request.Steps.Count; i++)
        {
            schedule.Steps.Add(new RolloutScheduleStep
            {
                Id = Guid.NewGuid(),
                RolloutScheduleId = schedule.Id,
                StepIndex = i,
                Percentage = request.Steps[i].Percentage,
                DurationMinutes = request.Steps[i].DurationMinutes,
                PromotedAt = i == 0 ? now : null
            });
        }

        context.RolloutSchedules.Add(schedule);

        // İlk adımın weight'lerini hemen uygula.
        RolloutWeightsApplier.ApplyStep(
            context, env,
            env.FeatureFlag.Type,
            schedule.TargetVariantId,
            request.Steps[0].Percentage,
            env.VariantWeights.ToList(),
            env.FeatureFlag.Variants.ToList());

        await context.SaveChangesAsync(ct);

        return Response<CreateRolloutScheduleDto>.Ok(new CreateRolloutScheduleDto(schedule.Id));
    }
}
