using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Switchly_2._0.WebApi.Models.Enums;
using Switchly_2._0.WebApi.Models.Shared;

namespace Switchly_2._0.WebApi.Auth;

public class UserContext:IUserContext
{
    public Guid UserId { get; }
    public IReadOnlyCollection<OrganizationRoleDto> Organizations { get; }
    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext
                          ?? throw new InvalidOperationException("HttpContext yok (UserContext).");

        var user = httpContext.User;
        if (user?.Identity is null || !user.Identity.IsAuthenticated)
            throw new InvalidOperationException("Kullanıcı authenticated değil.");

        // userId (sub / nameidentifier)
        var userIdClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)
                          ?? user.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            throw new InvalidOperationException("Token içinde geçerli UserId yok.");

        UserId = userId;

        // org claim'lerini oku
        var orgClaims = user.FindAll("org");
        var organizations = new List<OrganizationRoleDto>();

        foreach (var claim in orgClaims)
        {
            // "orgId:Role" formatını parse et
            var parts = claim.Value.Split(':', 2);
            if (parts.Length != 2) continue;

            if (!Guid.TryParse(parts[0], out var orgId))
                continue;

            var role = parts[1];
            organizations.Add(new OrganizationRoleDto(orgId, role));
        }

        Organizations = organizations;
    }

    public bool IsInRole(Guid organizationId, string role)
        => Organizations.Any(o =>
            o.OrganizationId == organizationId &&
            string.Equals(o.Role, role, StringComparison.OrdinalIgnoreCase));

    public bool IsOwner(Guid organizationId)
        => IsInRole(organizationId, "Owner");
}