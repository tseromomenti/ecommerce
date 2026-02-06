using System.Text;
using Ecommerce.ServiceDefaults.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.ServiceDefaults.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEcommerceJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AuthorizationOptions>? configureAuthorization = null)
    {
        var jwtSettings = ReadJwtSettings(configuration);
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        if (configureAuthorization is null)
        {
            services.AddAuthorization();
        }
        else
        {
            services.AddAuthorization(configureAuthorization);
        }

        return services;
    }

    public static IServiceCollection AddEcommerceCors(
        this IServiceCollection services,
        IConfiguration configuration,
        string policyName = ServiceDefaultsConstants.DefaultCorsPolicyName)
    {
        var corsSettings = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();

        var configuredOrigins = corsSettings.AllowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var origins = configuredOrigins.Length == 0
            ? ["http://localhost:4200"]
            : configuredOrigins;

        services.AddCors(options =>
        {
            options.AddPolicy(policyName, builder =>
            {
                if (corsSettings.AllowAnyOrigin)
                {
                    builder.AllowAnyOrigin();
                }
                else
                {
                    builder.WithOrigins(origins);
                }

                builder.AllowAnyMethod();
                builder.AllowAnyHeader();
            });
        });

        return services;
    }

    public static void AddAdminPolicy(AuthorizationOptions options)
    {
        options.AddPolicy(ServiceDefaultsConstants.AdminPolicyName, policy => policy.RequireRole("Admin"));
    }

    private static JwtSettings ReadJwtSettings(IConfiguration configuration)
    {
        var settings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

        if (string.IsNullOrWhiteSpace(settings.SigningKey) || settings.SigningKey.Length < 16)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be configured with at least 16 characters.");
        }

        return settings;
    }
}
