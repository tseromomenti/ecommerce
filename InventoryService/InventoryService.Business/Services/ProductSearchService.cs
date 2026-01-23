using InventoryService.Business.Interfaces;

namespace InventoryService.Business.Services;

/// <summary>
/// Hybrid product search service combining keyword matching and semantic vector search
/// </summary>
public class ProductSearchService(
    IInventoryRepository inventoryRepository,
    IVectorSearchService vectorSearchService,
    ILogger<ProductSearchService> logger) : IProductSearchService
{
    private const double KeywordWeight = 0.4;
    private const double SemanticWeight = 0.6;

    public async Task<IEnumerable<ProductSearchResult>> SearchProductsAsync(string query, int maxResults = 10)
    {
        try
        {
            logger.LogInformation("Performing hybrid search for query: {Query}", query);

            // Execute keyword and semantic search in parallel
            var keywordTask = PerformKeywordSearchAsync(query, maxResults * 2);
            var semanticTask = PerformSemanticSearchAsync(query, maxResults * 2);

            await Task.WhenAll(keywordTask, semanticTask);

            var keywordResults = await keywordTask;
            var semanticResults = await semanticTask;

            // Merge results using reciprocal rank fusion
            var hybridResults = MergeResults(keywordResults, semanticResults, maxResults);

            logger.LogInformation("Hybrid search returned {Count} results for query: {Query}", hybridResults.Count(), query);
            return hybridResults;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in hybrid search for query: {Query}", query);
            
            // Fallback to keyword-only search if hybrid fails
            return await PerformKeywordSearchAsync(query, maxResults);
        }
    }

    private async Task<IEnumerable<ProductSearchResult>> PerformKeywordSearchAsync(string query, int maxResults)
    {
        try
        {
            var allProducts = await inventoryRepository.GetAllProductsAsync();
            var queryLower = query.ToLowerInvariant();
            var queryTerms = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var keywordResults = allProducts
                .Select(p => new
                {
                    Product = p,
                    Score = CalculateKeywordScore(p.ProductName, queryLower, queryTerms)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(maxResults)
                .Select(x => new ProductSearchResult
                {
                    Id = x.Product.Id,
                    ProductName = x.Product.ProductName,
                    Price = x.Product.Price,
                    AvailableStock = x.Product.AvailableStock,
                    RelevanceScore = x.Score
                });

            return keywordResults;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Keyword search failed for query: {Query}", query);
            return Enumerable.Empty<ProductSearchResult>();
        }
    }

    private async Task<IEnumerable<ProductSearchResult>> PerformSemanticSearchAsync(string query, int maxResults)
    {
        try
        {
            var vectorResults = await vectorSearchService.SemanticSearchAsync(query, maxResults);

            return vectorResults.Select(vr => new ProductSearchResult
            {
                Id = vr.ProductId,
                ProductName = vr.ProductName,
                Price = vr.Price,
                AvailableStock = vr.AvailableStock,
                RelevanceScore = vr.SimilarityScore
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Semantic search failed for query: {Query}. Falling back to keyword search only.", query);
            return Enumerable.Empty<ProductSearchResult>();
        }
    }

    private static double CalculateKeywordScore(string productName, string query, string[] queryTerms)
    {
        var nameLower = productName.ToLowerInvariant();
        double score = 0;

        // Exact match gets highest score
        if (nameLower.Equals(query, StringComparison.OrdinalIgnoreCase))
        {
            return 1.0;
        }

        // Contains full query
        if (nameLower.Contains(query))
        {
            score += 0.8;
        }

        // Check individual terms
        foreach (var term in queryTerms)
        {
            if (nameLower.Contains(term))
            {
                score += 0.3 / queryTerms.Length;
            }
        }

        // Starts with query bonus
        if (nameLower.StartsWith(query))
        {
            score += 0.1;
        }

        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Merges keyword and semantic results using Reciprocal Rank Fusion (RRF)
    /// </summary>
    private IEnumerable<ProductSearchResult> MergeResults(
        IEnumerable<ProductSearchResult> keywordResults,
        IEnumerable<ProductSearchResult> semanticResults,
        int maxResults)
    {
        const int k = 60; // RRF constant

        var keywordDict = keywordResults
            .Select((r, index) => new { r.Id, Rank = index + 1, Result = r })
            .ToDictionary(x => x.Id, x => (x.Rank, x.Result));

        var semanticDict = semanticResults
            .Select((r, index) => new { r.Id, Rank = index + 1, Result = r })
            .ToDictionary(x => x.Id, x => (x.Rank, x.Result));

        // Get all unique product IDs
        var allIds = keywordDict.Keys.Union(semanticDict.Keys);

        var fusedResults = allIds.Select(id =>
        {
            // Calculate RRF score
            double rrfScore = 0;
            ProductSearchResult? result = null;

            if (keywordDict.TryGetValue(id, out var keywordMatch))
            {
                rrfScore += KeywordWeight * (1.0 / (k + keywordMatch.Rank));
                result = keywordMatch.Result;
            }

            if (semanticDict.TryGetValue(id, out var semanticMatch))
            {
                rrfScore += SemanticWeight * (1.0 / (k + semanticMatch.Rank));
                result ??= semanticMatch.Result;
                
                // Update stock/price from semantic if keyword didn't have it
                if (result.AvailableStock == 0 && semanticMatch.Result.AvailableStock > 0)
                {
                    result.AvailableStock = semanticMatch.Result.AvailableStock;
                }
            }

            return (Result: result!, RrfScore: rrfScore);
        })
        .OrderByDescending(x => x.RrfScore)
        .Take(maxResults)
        .Select(x =>
        {
            x.Result.RelevanceScore = x.RrfScore;
            return x.Result;
        });

        return fusedResults;
    }
}

