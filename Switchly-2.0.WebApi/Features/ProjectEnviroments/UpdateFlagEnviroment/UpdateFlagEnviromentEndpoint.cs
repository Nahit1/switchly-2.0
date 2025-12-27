using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Switchly_2._0.WebApi.Features.Organizations.CreateOrganization;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.ProjectEnviroments.UpdateFlagEnviroment;

public class UpdateFlagEnviromentEndpoint:CarterModule
{
    
    public sealed class Request
    {
        public Guid FeatureFlagId { get; set; }
        public Guid ProjectEnvironmentId { get; set; }
        public bool IsEnabled { get; set; }
        public RolloutKind DefaultRolloutKind { get; set; }
        public int DefaultRolloutPercentage { get; set; }
        
    }
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/environments/update-flag-enviroment", async ([FromBody] Request r, IMediator mediator) =>
            {
                // MVP: Password -> “hash” gibi saklandı; gerçek projede hashing ekle
                var cmd = new UpdateFlagEnvironmentRequest(r.FeatureFlagId, r.ProjectEnvironmentId, r.IsEnabled, r.DefaultRolloutKind, r.DefaultRolloutPercentage);
                var res = await mediator.Send(cmd);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Environments")
            .WithName("UpdateFlagEnviroment")
            .RequireAuthorization();
    }
    
    
}