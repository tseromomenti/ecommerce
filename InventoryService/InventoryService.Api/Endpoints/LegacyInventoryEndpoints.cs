using InventoryService.Business.Interfaces;

namespace InventoryService.Api.Endpoints;

internal static class LegacyInventoryEndpoints
{
    public static IEndpointRouteBuilder MapLegacyInventoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/GetProductHistory", async (string productName, IInventoryManagementService inventoryService) =>
        {
            var productHistory = await inventoryService.GetProductHistoryAsync(productName);
            return Results.Ok(productHistory);
        });

        endpoints.MapGet("/GetAllProducts", async (IInventoryManagementService inventoryService) =>
        {
            var products = await inventoryService.GetAllProductsAsync();
            return Results.Ok(products);
        });

        endpoints.MapGet("/GetProductById", async (int productId, IInventoryManagementService inventoryService) =>
        {
            var product = await inventoryService.GetProductByIdAsync(productId);
            return product == null
                ? Results.NotFound(new { message = $"Product with ID {productId} not found." })
                : Results.Ok(product);
        });

        endpoints.MapGet("/SearchProducts", async (string query, int? maxResults, IProductSearchService searchService) =>
        {
            var results = await searchService.SearchProductsAsync(query, maxResults ?? 10);
            return Results.Ok(results);
        });

        endpoints.MapGet("/SemanticSearch", async (string query, int? maxResults, IVectorSearchService vectorSearchService) =>
        {
            var results = await vectorSearchService.SemanticSearchAsync(query, maxResults ?? 10);
            return Results.Ok(results);
        });

        endpoints.MapPost("/IndexProducts", async (IVectorSearchService vectorSearchService) =>
        {
            await vectorSearchService.IndexAllProductsAsync();
            return Results.Ok(new { message = "Products indexed successfully" });
        });

        return endpoints;
    }
}
