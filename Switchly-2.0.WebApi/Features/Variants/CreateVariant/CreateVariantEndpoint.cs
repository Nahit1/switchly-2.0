using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Switchly_2._0.WebApi.Features.Variants.GetVariantsByFlag;

namespace Switchly_2._0.WebApi.Features.Variants.CreateVariant;

public class CreateVariantEndpoint:CarterModule
{
    public sealed record CreateVariantBody(
        Guid OrganizationId,
        Guid ProjectId,
        string Key,
        string? Name,
        string? PayloadJson
    );
    
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/flags/{flagId:guid}/variants", async (
                Guid flagId,
                [FromBody] CreateVariantBody body,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var res = await mediator.Send(
                    new CreateVariantCommand(body.OrganizationId, body.ProjectId, flagId, body.Key, body.Name, body.PayloadJson),
                    ct);

                return res.Success ? Results.Ok(res) : Results.BadRequest(res);
            })
            .WithTags("Variants")
            .WithName("CreateVariant")
            .RequireAuthorization();
    }
    
}