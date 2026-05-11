using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Auth;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Common;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.SegmetRules.CreateSegmentRules;
public sealed record CreateSegmentGroupCommand(
    Guid OrganizationId,
    string Key,
    string Name,
    string? Description,
    LogicalOperator LogicalOperator
) : IRequest<Response<Guid>>;

public sealed class CreateSegmentGroupHandler(
    SwitchlyDbContext context,
    IUserContext userContext
) : IRequestHandler<CreateSegmentGroupCommand, Response<Guid>>
{
    public async Task<Response<Guid>> Handle(CreateSegmentGroupCommand request, CancellationToken ct)
    {
        // AuthZ: user must be a member of the organization
        var isMember = await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m => m.OrganizationId == request.OrganizationId && m.UserId == userContext.UserId, ct);

        if (!isMember)
            return Response<Guid>.Fail("Bu organization için yetkin yok.");

        var key = (request.Key ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(key))
            return Response<Guid>.Fail("Segment key zorunludur.");

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Response<Guid>.Fail("Segment adı zorunludur.");

        // Uniqueness: within an organization, SegmentGroup key should be unique
        var exists = await context.SegmentGroups
            .AsNoTracking()
            .AnyAsync(sg => sg.OrganizationId == request.OrganizationId && sg.Key == key, ct);

        if (exists)
            return Response<Guid>.Fail("Bu key ile daha önce segment oluşturulmuş.");

        var entity = new SegmentGroup
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            Key = key,
            Name = name,
            Description = request.Description?.Trim(),
            LogicalOperator = request.LogicalOperator,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.SegmentGroups.Add(entity);
        await context.SaveChangesAsync(ct);

        return Response<Guid>.Ok(entity.Id, "Segment group created");
    }
}