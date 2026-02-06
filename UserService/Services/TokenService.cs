using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using UserService.Entities;

namespace UserService.Services;

public class TokenService(IConfiguration configuration) : ITokenService
{
    public Task<(string AccessToken, DateTime ExpiresAtUtc)> CreateAccessTokenAsync(ApplicationUser user, IReadOnlyCollection<string> roles)
    {
        var issuer = configuration["Jwt:Issuer"] ?? "ECommerceOrderingSystem";
        var audience = configuration["Jwt:Audience"] ?? "ECommerceOrderingSystem.Client";
        var key = configuration["Jwt:SigningKey"] ?? "super-secret-dev-signing-key-change-me";
        var expiryMinutes = int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var parsed) ? parsed : 15;
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return Task.FromResult((new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc));
    }

    public string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
