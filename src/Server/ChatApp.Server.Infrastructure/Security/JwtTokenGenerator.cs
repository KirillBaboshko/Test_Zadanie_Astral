using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ChatApp.Server.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ChatApp.Server.Infrastructure.Security;


public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;
    private readonly RsaSecurityKey _securityKey;

    public JwtTokenGenerator(IConfiguration configuration, RsaSecurityKey securityKey)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _securityKey = securityKey ?? throw new ArgumentNullException(nameof(securityKey));
    }

    /// <summary>
    /// Генерирует JWT токен для пользователя
    /// </summary>
    public String GenerateToken(Guid userId, String username)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username)
        };

        var credentials = new SigningCredentials(_securityKey, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
