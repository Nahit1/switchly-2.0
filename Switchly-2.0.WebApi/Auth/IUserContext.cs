using Switchly_2._0.WebApi.Models.Shared;

namespace Switchly_2._0.WebApi.Auth;

public interface IUserContext
{
    Guid UserId { get; }
    IReadOnlyCollection<OrganizationRoleDto> Organizations { get; }

    bool IsInRole(Guid organizationId, string role);
    bool IsOwner(Guid organizationId);
}