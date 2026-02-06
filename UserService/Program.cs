using Ecommerce.ServiceDefaults;
using Ecommerce.ServiceDefaults.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using UserService.Data;
using UserService.Entities;
using UserService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddDbContext<UserDbContext>(options =>
{
    var connection = builder.Configuration.GetConnectionString("DbConnection")
                     ?? "Server=localhost,1433;Database=UsersDb;User Id=sa;Password=ComplexPassword123!;TrustServerCertificate=True;Encrypt=False;";
    options.UseSqlServer(connection);
});

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<UserDbContext>()
    .AddSignInManager<SignInManager<ApplicationUser>>();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddEcommerceJwtAuthentication(builder.Configuration, options =>
{
    options.AddPolicy(ServiceDefaultsConstants.AdminPolicyName, policy => policy.RequireRole("Admin"));
});
builder.Services.AddEcommerceCors(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    dbContext.Database.EnsureCreated();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    await UserSeed.SeedAsync(roleManager, userManager, app.Configuration);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseEcommerceApiPipeline(ServiceDefaultsConstants.DefaultCorsPolicyName);
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
