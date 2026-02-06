using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Ecommerce.ServiceDefaults.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCorrelationId(
        this IApplicationBuilder app,
        string headerName = ServiceDefaultsConstants.CorrelationIdHeaderName)
    {
        return app.Use(async (context, next) =>
        {
            if (!context.Request.Headers.TryGetValue(headerName, out var correlationId) ||
                string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString("N");
                context.Request.Headers[headerName] = correlationId;
            }

            context.Response.Headers[headerName] = correlationId.ToString();
            await next();
        });
    }

    public static WebApplication UseEcommerceApiPipeline(
        this WebApplication app,
        string? corsPolicyName = ServiceDefaultsConstants.DefaultCorsPolicyName,
        bool useHttpsRedirection = false)
    {
        if (useHttpsRedirection)
        {
            app.UseHttpsRedirection();
        }

        if (!string.IsNullOrWhiteSpace(corsPolicyName))
        {
            app.UseCors(corsPolicyName);
        }

        app.UseCorrelationId();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
