using InventoryService.Business.Entities;

namespace InventoryService.Business.Interfaces;

public interface IProductSearchService
{
    Task<IEnumerable<ProductSearchResult>> SearchProductsAsync(string query, int maxResults = 10);
}

public class ProductSearchResult
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int AvailableStock { get; set; }
    public double RelevanceScore { get; set; }
}
