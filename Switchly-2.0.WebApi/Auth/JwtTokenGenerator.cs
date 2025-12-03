using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Switchly_2._0.WebApi.Models.Shared;

namespace Switchly_2._0.WebApi.Auth;

public class JwtTokenGenerator(IOptions<JwtSettings> options): IJwtTokenGenerator
{
    private readonly JwtSettings _settings = options.Value;
    public string GenerateToken(Guid userId, IEnumerable<OrganizationRoleDto> organizations)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Org + role claim’leri (ileride claim türünü değiştirebilirsin)
        foreach (var org in organizations)
        {
            claims.Add(new Claim("org", $"{org.OrganizationId}:{org.Role}"));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}