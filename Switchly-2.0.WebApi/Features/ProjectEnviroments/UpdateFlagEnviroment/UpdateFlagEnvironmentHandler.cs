using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Features.Organizations.CreateOrganization;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.ProjectEnviroments.UpdateFlagEnviroment;

public record UpdateFlagEnvironmentRequest(
    Guid FeatureFlagId,
    Guid ProjectEnvironmentId,
    bool IsEnabled,
    RolloutKind DefaultRolloutKind, // AllUsers, Percentage, Off
    int DefaultRolloutPercentage    // 0–100, sadece Percentage ise anlamlı
):IRequest<Response<string>>;

public class UpdateFlagEnvironmentHandler(SwitchlyDbContext context, IUserContext userContext)
    : IRequestHandler<UpdateFlagEnvironmentRequest, Response<string>>
{
    public async Task<Response<string>> Handle(UpdateFlagEnvironmentRequest request, CancellationToken cancellationToken)
    {
        var entity = await context.FeatureFlagEnvironments
            .FirstOrDefaultAsync(x =>
                    x.FeatureFlagId == request.FeatureFlagId &&
                    x.ProjectEnvironmentId == request.ProjectEnvironmentId,
                cancellationToken);
        
        if (entity is null)
        {
            entity = new FeatureFlagEnvironment
            {
                Id = Guid.NewGuid(),
                FeatureFlagId = request.FeatureFlagId,
                ProjectEnvironmentId = request.ProjectEnvironmentId
            };
            context.FeatureFlagEnvironments.Add(entity);
        }

        entity.IsEnabled = request.IsEnabled;
        entity.DefaultRolloutKind = request.DefaultRolloutKind;
        entity.DefaultRolloutPercentage = request.DefaultRolloutPercentage;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        
        return Response<string>.Ok("Updated flag environment.");
    }
}