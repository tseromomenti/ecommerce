using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Entities;
using UserService.Models;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    UserDbContext dbContext,
    ITokenService tokenService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var userExists = await userManager.FindByEmailAsync(normalizedEmail);
        if (userExists != null)
        {
            return Conflict(new { message = "Email already registered." });
        }

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? normalizedEmail : request.DisplayName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        await userManager.AddToRoleAsync(user, "Customer");

        var roles = await userManager.GetRolesAsync(user);
        var tokenPair = await tokenService.CreateAccessTokenAsync(user, roles.ToList());
        var refreshToken = tokenService.CreateRefreshToken();
        dbContext.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenService.HashToken(refreshToken),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        });
        await dbContext.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            AccessToken = tokenPair.AccessToken,
            AccessTokenExpiresAtUtc = tokenPair.ExpiresAtUtc,
            RefreshToken = refreshToken,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Roles = roles.ToList()
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!signInResult.Succeeded)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var roles = await userManager.GetRolesAsync(user);
        var tokenPair = await tokenService.CreateAccessTokenAsync(user, roles.ToList());
        var refreshToken = tokenService.CreateRefreshToken();

        dbContext.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenService.HashToken(refreshToken),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        });
        await dbContext.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            AccessToken = tokenPair.AccessToken,
            AccessTokenExpiresAtUtc = tokenPair.ExpiresAtUtc,
            RefreshToken = refreshToken,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Roles = roles.ToList()
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request)
    {
        var hash = tokenService.HashToken(request.RefreshToken);
        var token = await dbContext.RefreshTokens
            .Where(t => t.TokenHash == hash)
            .OrderByDescending(t => t.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (token == null || token.IsRevoked || token.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return Unauthorized(new { message = "Invalid refresh token." });
        }

        var user = await userManager.FindByIdAsync(token.UserId);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid refresh token." });
        }

        token.RevokedAtUtc = DateTime.UtcNow;

        var roles = await userManager.GetRolesAsync(user);
        var tokenPair = await tokenService.CreateAccessTokenAsync(user, roles.ToList());
        var newRefresh = tokenService.CreateRefreshToken();

        dbContext.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenService.HashToken(newRefresh),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        });

        await dbContext.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            AccessToken = tokenPair.AccessToken,
            AccessTokenExpiresAtUtc = tokenPair.ExpiresAtUtc,
            RefreshToken = newRefresh,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Roles = roles.ToList()
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var hash = tokenService.HashToken(request.RefreshToken);
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (token != null && !token.IsRevoked)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        return Ok(new { message = "Logged out." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me()
    {
        var userId = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await userManager.GetRolesAsync(user);
        return Ok(new MeResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Roles = roles.ToList()
        });
    }
}
