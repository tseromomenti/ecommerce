using InventoryService.Business.Interfaces;

namespace InventoryService.Business.Services;

public class ProductSearchService(
    IInventoryRepository inventoryRepository,
    ILogger<ProductSearchService> logger) : IProductSearchService
{
    public async Task<IEnumerable<ProductSearchResult>> SearchProductsAsync(string query, int maxResults = 10)
    {
        try
        {
            var allProducts = await inventoryRepository.GetAllProductsAsync();
            
            var keywordResults = allProducts
                .Where(p => p.ProductName.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(p => new ProductSearchResult
                {
                    Id = p.Id,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    AvailableStock = p.AvailableStock,
                    RelevanceScore = p.ProductName.Equals(query, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.5
                })
                .OrderByDescending(r => r.RelevanceScore)
                .Take(maxResults);

            return keywordResults;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching products for query: {Query}", query);
            return Enumerable.Empty<ProductSearchResult>();
        }
    }
}
