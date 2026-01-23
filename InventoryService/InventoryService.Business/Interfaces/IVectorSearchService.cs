using Ecommerce.Library.VectorData;

namespace InventoryService.Business.Interfaces;

/// <summary>
/// Service for vector-based semantic search using embeddings
/// </summary>
public interface IVectorSearchService
{
    /// <summary>
    /// Performs semantic search using vector embeddings
    /// </summary>
    /// <param name="query">Natural language search query</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <returns>List of matching items with similarity scores</returns>
    Task<IEnumerable<VectorSearchResult>> SemanticSearchAsync(string query, int maxResults = 10);

    /// <summary>
    /// Indexes a product into the vector store with embeddings
    /// </summary>
    Task IndexProductAsync(int productId, string productName, string description, decimal price, int availableStock);

    /// <summary>
    /// Indexes all products from the database into the vector store
    /// </summary>
    Task IndexAllProductsAsync();

    /// <summary>
    /// Removes a product from the vector index
    /// </summary>
    Task RemoveProductFromIndexAsync(int productId);
}

public class VectorSearchResult
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int AvailableStock { get; set; }
    public double SimilarityScore { get; set; }
}
