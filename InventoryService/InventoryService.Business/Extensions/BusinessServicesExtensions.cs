using InventoryService.Business.Interfaces;
using InventoryService.Business.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InventoryService.Business.Extensions
{
    public static class BusinessServiceExtensions
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services, IHostApplicationBuilder builder)
        {
            services.AddScoped<IInventoryManagementService, InventoryManagementService>();
            services.AddScoped<IVectorSearchService, VectorSearchService>();
            services.AddScoped<IProductSearchService, ProductSearchService>();

            return services;
        }
    }
}
