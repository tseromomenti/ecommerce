using InventoryService.Business.Interfaces;
using InventoryService.Persistance.Infrastructure;
using InventoryService.Persistance.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InventoryService.Persistance.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPersistance(this IServiceCollection services, IHostApplicationBuilder builder)
        {
            // Register AutoMapper with persistence mapping profile
            services.AddAutoMapper(typeof(PersistenceMappingProfile));

            // Register DbContext for SQL Server
            services.AddDbContext<InventoryDbContext>(options =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection"));
                }
                else
                {
                    options.UseAzureSql(builder.Configuration.GetConnectionString("DbConnection"));
                }
            });

            // Register repositories
            services.AddScoped<IInventoryRepository, InventoryRepository>();
            services.AddScoped<IDocumentRepository, DocumentRepository>();

            // Register MongoDB context
            services.AddSingleton(_ =>
            {
                var connectionString = builder.Configuration.GetConnectionString("MongoDb");
                var databaseName = builder.Configuration["MongoDb:DatabaseName"];
                return new MongoDbContext(connectionString ?? throw new InvalidOperationException("MongoDb connection string is required"),
                                        databaseName ?? throw new InvalidOperationException("MongoDb database name is required"));
            });

            return services;
        }
    }
}