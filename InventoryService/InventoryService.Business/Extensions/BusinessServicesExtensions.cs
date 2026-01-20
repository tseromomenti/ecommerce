using InventoryService.Business.Interfaces;
using InventoryService.Business.Services;
using Microsoft.AspNetCore.Builder;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryService.Business.Extensions
{
    public static class BusinessServiceExtensions
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services, IHostApplicationBuilder builder)
        {
            services.AddScoped<IInventoryManagementService, InventoryManagementService>();
            services.AddScoped<IProductSearchService, ProductSearchService>();

            return services;
        }
    }
}
