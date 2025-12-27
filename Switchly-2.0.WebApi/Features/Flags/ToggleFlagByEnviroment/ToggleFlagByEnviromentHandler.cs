using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Models.Common;

namespace Switchly_2._0.WebApi.Features.Flags.ToggleFlagByEnviroment;

public sealed record ToggleFlagEnvironmentCommand(
    Guid OrganizationId,
    Guid ProjectId,
    Guid FeatureFlagId,
    Guid ProjectEnvironmentId,
    bool IsEnabled
) : IRequest<Response<bool>>;

public class ToggleFlagByEnviromentHandler(
    SwitchlyDbContext context,
    IUserContext userContext // senin CurrentUser abstraction'ın
) : IRequestHandler<ToggleFlagEnvironmentCommand, Response<bool>>
{
    public async Task<Response<bool>> Handle(ToggleFlagEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var isMember = await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m =>
                m.OrganizationId == request.OrganizationId &&
                m.UserId == userContext.UserId, cancellationToken);

        if (!isMember)
            return Response<bool>.Fail("Bu organization için yetkin yok.");
        
        var valid = await context.FeatureFlagEnvironments
            .AsNoTracking()
            .AnyAsync(x =>
                    x.FeatureFlagId == request.FeatureFlagId &&
                    x.ProjectEnvironmentId == request.ProjectEnvironmentId &&
                    x.FeatureFlag.ProjectId == request.ProjectId &&
                    x.ProjectEnvironment.ProjectId == request.ProjectId,
                cancellationToken);

        if (!valid)
            return Response<bool>.Fail("Flag ve environment bu project'e ait değil.");

        var entity = await context.FeatureFlagEnvironments
            .FirstOrDefaultAsync(x =>
                x.FeatureFlagId == request.FeatureFlagId &&
                x.ProjectEnvironmentId == request.ProjectEnvironmentId, cancellationToken);

        if (entity is null)
            return Response<bool>.Fail("FeatureFlagEnvironment kaydı bulunamadı.");

        // ✅ SADECE toggle
        entity.IsEnabled = request.IsEnabled;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Response<bool>.Ok(true, "Updated");
    }
}