using Switchly_2._0.WebApi.Models.Shared;

namespace Switchly_2._0.WebApi.Auth;

public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, IEnumerable<OrganizationRoleDto> organizations);
}