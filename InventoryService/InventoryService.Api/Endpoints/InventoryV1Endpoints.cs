using InventoryService.Api.Contracts;
using InventoryService.Business.Interfaces;

namespace InventoryService.Api.Endpoints;

internal static class InventoryV1Endpoints
{
    public static IEndpointRouteBuilder MapInventoryV1Endpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/inventory/search/hybrid", async (InventorySearchRequest request, IProductSearchService searchService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return Results.BadRequest(new { message = "Query is required." });
            }

            var results = await searchService.SearchProductsAsync(
                request.Query,
                request.Filters ?? new SearchFilters(),
                request.MaxResults is > 0 ? request.MaxResults.Value : 10);

            return Results.Ok(results);
        });

        endpoints.MapPost("/api/v1/inventory/search/semantic", async (InventorySearchRequest request, IVectorSearchService vectorSearchService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return Results.BadRequest(new { message = "Query is required." });
            }

            var results = await vectorSearchService.SemanticSearchAsync(request.Query, request.MaxResults ?? 10);
            return Results.Ok(results);
        });

        endpoints.MapGet("/api/v1/inventory/products/{productId:int}", async (int productId, IInventoryManagementService inventoryService) =>
        {
            var product = await inventoryService.GetProductByIdAsync(productId);
            return product == null ? Results.NotFound() : Results.Ok(product);
        });

        return endpoints;
    }
}
