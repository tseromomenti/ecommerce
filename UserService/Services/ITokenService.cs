using UserService.Entities;

namespace UserService.Services;

public interface ITokenService
{
    Task<(string AccessToken, DateTime ExpiresAtUtc)> CreateAccessTokenAsync(ApplicationUser user, IReadOnlyCollection<string> roles);
    string CreateRefreshToken();
    string HashToken(string token);
}
