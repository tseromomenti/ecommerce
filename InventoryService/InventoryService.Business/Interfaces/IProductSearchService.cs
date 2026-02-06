using InventoryService.Business.Entities;

namespace InventoryService.Business.Interfaces;

public interface IProductSearchService
{
    Task<IEnumerable<ProductSearchResult>> SearchProductsAsync(string query, int maxResults = 10);
    Task<IEnumerable<ProductSearchResult>> SearchProductsAsync(string query, SearchFilters filters, int maxResults = 10);
}

public class ProductSearchResult
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Subcategory { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int AvailableStock { get; set; }
    public double RelevanceScore { get; set; }
}

public class SearchFilters
{
    public string? Category { get; set; }
    public string? Subcategory { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }
    public string? Brand { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool InStockOnly { get; set; } = true;
    public List<string>? PersonaTags { get; set; }
}
