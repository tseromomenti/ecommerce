using Ecommerce.Library.VectorData;
using InventoryService.Business.Entities;
using InventoryService.Business.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;

namespace InventoryService.Business.Services;

/// <summary>
/// Vector search service using Semantic Kernel, Qdrant, and Ollama nomic-embed-text
/// </summary>
public class VectorSearchService : IVectorSearchService
{
    private readonly VectorStore _vectorStore;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<VectorSearchService> _logger;
    private const string CollectionName = "inventory_items";

    public VectorSearchService(
        VectorStore vectorStore,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IInventoryRepository inventoryRepository,
        ILogger<VectorSearchService> logger)
    {
        _vectorStore = vectorStore;
        _embeddingGenerator = embeddingGenerator;
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<VectorSearchResult>> SemanticSearchAsync(string query, int maxResults = 10)
    {
        try
        {
            _logger.LogInformation("Performing semantic search for query: {Query}", query);

            // Get the vector collection
            var collection = _vectorStore.GetCollection<ulong, InventoryItem>(CollectionName);
            await collection.EnsureCollectionExistsAsync();

            // Generate embedding for the search query
            var queryEmbedding = await _embeddingGenerator.GenerateAsync([query]);

            // Perform vector search
            var searchResults = await collection.SearchAsync(
                queryEmbedding[0].Vector,
                maxResults
            ).ToListAsync();

            var results = searchResults.Select(r => new VectorSearchResult
            {
                ProductId = (int)r.Record.ItemId,
                ProductName = r.Record.ItemName,
                Description = r.Record.Description,
                ImageUrl = ProductImageResolver.GetImageUrl(r.Record.ItemName),
                Price = (decimal)r.Record.Price,
                AvailableStock = r.Record.AvailableStock,
                SimilarityScore = r.Score ?? 0
            }).ToList();

            _logger.LogInformation("Semantic search returned {Count} results for query: {Query}", results.Count, query);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing semantic search for query: {Query}", query);
            return Enumerable.Empty<VectorSearchResult>();
        }
    }

    public async Task IndexProductAsync(int productId, string productName, string description, decimal price, int availableStock)
    {
        try
        {
            _logger.LogInformation("Indexing product {ProductId}: {ProductName}", productId, productName);

            var collection = _vectorStore.GetCollection<ulong, InventoryItem>(CollectionName);
            await collection.EnsureCollectionExistsAsync();

            // Create searchable text by combining product name and description
            var searchableText = $"{productName}. {description}";
            
            // Generate embedding
            var embeddings = await _embeddingGenerator.GenerateAsync([searchableText]);

            var item = new InventoryItem
            {
                ItemId = (ulong)productId,
                ItemName = productName,
                Description = description,
                Price = (int)price,
                AvailableStock = availableStock,
                ItemEmbedding = embeddings[0].Vector
            };

            await collection.UpsertAsync(item);
            _logger.LogInformation("Successfully indexed product {ProductId}", productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing product {ProductId}", productId);
            throw;
        }
    }

    public async Task IndexAllProductsAsync()
    {
        try
        {
            _logger.LogInformation("Starting to index all products");

            var products = await _inventoryRepository.GetAllProductsAsync();
            var collection = _vectorStore.GetCollection<ulong, InventoryItem>(CollectionName);
            await collection.EnsureCollectionExistsAsync();

            foreach (var product in products)
            {
                // Prefer authored description and append metadata for retrieval quality.
                var description = string.IsNullOrWhiteSpace(product.Description)
                    ? GenerateProductDescription(product.ProductName, product.Price, product.AvailableStock)
                    : $"{product.Description} Category: {product.Category}. Brand: {product.Brand}. Tags: {product.Tags}.";
                
                await IndexProductAsync(
                    product.Id,
                    product.ProductName,
                    description,
                    product.Price,
                    product.AvailableStock
                );
            }

            _logger.LogInformation("Successfully indexed {Count} products", products.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing all products");
            throw;
        }
    }

    public async Task RemoveProductFromIndexAsync(int productId)
    {
        try
        {
            _logger.LogInformation("Removing product {ProductId} from vector index", productId);

            var collection = _vectorStore.GetCollection<ulong, InventoryItem>(CollectionName);
            await collection.EnsureCollectionExistsAsync();
            await collection.DeleteAsync((ulong)productId);

            _logger.LogInformation("Successfully removed product {ProductId} from index", productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing product {ProductId} from index", productId);
            throw;
        }
    }

    private static string GenerateProductDescription(string productName, decimal price, int stock)
    {
        // Generate a simple description for products without explicit descriptions
        var availability = stock > 50 ? "widely available" : stock > 10 ? "in stock" : stock > 0 ? "limited stock" : "out of stock";
        var priceCategory = price > 100 ? "premium" : price > 50 ? "mid-range" : "affordable";
        
        return $"{productName} is a {priceCategory} product priced at ${price:F2}. Currently {availability} with {stock} units.";
    }
}
