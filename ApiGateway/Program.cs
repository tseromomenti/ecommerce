using System.Threading.RateLimiting;
using Ecommerce.ServiceDefaults;
using Ecommerce.ServiceDefaults.Extensions;
using Microsoft.AspNetCore.Authentication;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEcommerceJwtAuthentication(builder.Configuration);
builder.Services.AddEcommerceCors(builder.Configuration);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
        RateLimitPartition.GetFixedWindowLimiter(
            "global",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 10,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

app.UseCors(ServiceDefaultsConstants.DefaultCorsPolicyName);
app.UseRateLimiter();
app.UseCorrelationId();
app.UseAuthentication();
app.UseAuthorization();

// Require JWT for v1 commerce APIs except auth and Stripe webhook.
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    var requiresAuth =
        path.StartsWith("/api/v1/", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/api/v1/auth/", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/api/v1/payments/webhooks/stripe", StringComparison.OrdinalIgnoreCase);

    if (requiresAuth && !(context.User?.Identity?.IsAuthenticated ?? false))
    {
        await context.ChallengeAsync();
        return;
    }

    await next();
});

app.MapControllers();
app.MapReverseProxy();
app.MapHealthChecks("/health");

app.Run();
