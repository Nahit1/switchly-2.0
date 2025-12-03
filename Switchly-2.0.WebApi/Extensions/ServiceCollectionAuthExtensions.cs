using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Switchly_2._0.WebApi.Auth;

namespace Switchly_2._0.WebApi.Extensions;

public static class ServiceCollectionAuthExtensions
{
    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        // JwtSettings’i options olarak al
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // AppSettings’ten direkt okuyalım
        var jwtSection = configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var key = jwtSection["SecretKey"];

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = signingKey
                };
            });
        
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();

        services.AddAuthorization(); // rol/org bazlı policy’ler için zemin
        
        

        return services;
    }
}