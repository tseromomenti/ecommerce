using Ecommerce.ServiceDefaults;
using InventoryService.Business.Entities;
using InventoryService.Business.Interfaces;

namespace InventoryService.Api.Endpoints;

internal static class AdminInventoryEndpoints
{
    public static IEndpointRouteBuilder MapAdminInventoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/admin/products", async (IInventoryManagementService inventoryService) =>
        {
            var products = await inventoryService.GetAllProductsAsync();
            return Results.Ok(products);
        }).RequireAuthorization(ServiceDefaultsConstants.AdminPolicyName);

        endpoints.MapPost("/api/v1/admin/products", async (ProductDto product, IInventoryRepository repository, IVectorSearchService vectorSearchService) =>
        {
            product.LastUpdated = DateTime.UtcNow;
            await repository.AddProductAsync(product);
            await vectorSearchService.IndexAllProductsAsync();
            return Results.Created($"/api/v1/inventory/products/{product.Id}", product);
        }).RequireAuthorization(ServiceDefaultsConstants.AdminPolicyName);

        endpoints.MapPut("/api/v1/admin/products/{productId:int}", async (int productId, ProductDto update, IInventoryRepository repository, IVectorSearchService vectorSearchService) =>
        {
            var existing = await repository.GetProductByIdAsync(productId);
            if (existing == null)
            {
                return Results.NotFound();
            }

            update.Id = productId;
            update.LastUpdated = DateTime.UtcNow;
            await repository.UpdateProductAsync(update);
            await vectorSearchService.IndexAllProductsAsync();
            return Results.Ok(update);
        }).RequireAuthorization(ServiceDefaultsConstants.AdminPolicyName);

        endpoints.MapDelete("/api/v1/admin/products/{productId:int}", async (int productId, IInventoryRepository repository, IVectorSearchService vectorSearchService) =>
        {
            var deleted = await repository.DeleteProductByIdAsync(productId);
            if (!deleted)
            {
                return Results.NotFound();
            }

            await vectorSearchService.RemoveProductFromIndexAsync(productId);
            return Results.NoContent();
        }).RequireAuthorization(ServiceDefaultsConstants.AdminPolicyName);

        return endpoints;
    }
}
