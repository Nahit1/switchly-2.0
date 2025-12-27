using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.Flags.CreateFlag;

public class CreateFlagEndpoint:CarterModule
{
    public sealed class Request
    {
        public Guid OrganizationId { get; set; }
        public Guid ProjectId { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public FeatureFlagType Type { get; set; }
    }
    
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/flag/create", async ([FromBody] Request r, IMediator mediator) =>
            {
                // MVP: Password -> “hash” gibi saklandı; gerçek projede hashing ekle
                var cmd = new CreateFlagCommandHandler(r.OrganizationId, r.ProjectId, r.Key,r.Name, r.Description, r.Type);
                var res = await mediator.Send(cmd);
                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Flag")
            .WithName("CreateFlag")
            .RequireAuthorization();
    }
}