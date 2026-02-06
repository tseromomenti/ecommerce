using InventoryService.Business.Interfaces;
using InventoryService.Persistance.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Api.Extensions;

internal static class StartupInitializationExtensions
{
    public static async Task InitializeInventoryAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        if (dbContext.Database.IsRelational())
        {
            dbContext.Database.Migrate();
        }

        InventoryDataSeeder.Seed(dbContext);

        try
        {
            var vectorSearchService = scope.ServiceProvider.GetRequiredService<IVectorSearchService>();
            await vectorSearchService.IndexAllProductsAsync();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("InventoryStartup");
            logger.LogWarning(ex, "Failed to index products on startup.");
        }
    }
}
