using InventoryService.Business.Interfaces;
using InventoryService.Business.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InventoryService.Business.Extensions;

public static class BusinessServiceExtensions
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IInventoryManagementService, InventoryManagementService>();
        services.AddScoped<IVectorSearchService, VectorSearchService>();
        services.AddScoped<IProductSearchService, ProductSearchService>();

        return services;
    }

    [Obsolete("Use AddBusinessServices() instead.")]
    public static IServiceCollection AddBusinessServices(this IServiceCollection services, IHostApplicationBuilder _)
        => services.AddBusinessServices();
}
