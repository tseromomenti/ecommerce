using Microsoft.AspNetCore.Identity;
using UserService.Entities;

namespace UserService.Data;

public static class UserSeed
{
    public static async Task SeedAsync(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        var roles = new[] { "Customer", "Admin" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminEmail = configuration["SeedAdmin:Email"] ?? "admin@local.dev";
        var adminPassword = configuration["SeedAdmin:Password"] ?? "Admin1234";
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin != null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            DisplayName = "System Admin",
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(admin, adminPassword);
        if (!createResult.Succeeded)
        {
            return;
        }

        await userManager.AddToRoleAsync(admin, "Admin");
    }
}
