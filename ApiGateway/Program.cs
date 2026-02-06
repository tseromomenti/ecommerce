
using Serilog;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

namespace ApiGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            builder.Services.AddControllers();

            builder.Services.AddHealthChecks();
            builder.Services.AddHttpContextAccessor();
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
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var issuer = builder.Configuration["Jwt:Issuer"] ?? "ECommerceOrderingSystem";
                    var audience = builder.Configuration["Jwt:Audience"] ?? "ECommerceOrderingSystem.Client";
                    var key = builder.Configuration["Jwt:SigningKey"] ?? "super-secret-dev-signing-key-change-me";
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });
            builder.Services.AddAuthorization();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAngular", policy =>
                {
                    policy.WithOrigins("http://localhost:4200")
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            builder.Host.UseSerilog((context, configuration) =>
                configuration.ReadFrom.Configuration(context.Configuration));

            builder.Host.UseSerilog();

            var app = builder.Build();

            app.UseCors("AllowAngular");
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

            app.Use(async (context, next) =>
            {
                if (!context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId) ||
                    string.IsNullOrWhiteSpace(correlationId))
                {
                    correlationId = Guid.NewGuid().ToString("N");
                    context.Request.Headers["X-Correlation-ID"] = correlationId;
                }

                context.Response.Headers["X-Correlation-ID"] = correlationId.ToString();
                await next();
            });

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

            app.MapReverseProxy();

            app.MapHealthChecks("/health");

            app.Run();
        }
    }
}
