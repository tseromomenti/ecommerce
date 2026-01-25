using InventoryService.Business.Entities;
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
    private const double MinRelativeToTop = 0.7; // keep results within 70% of top score
    private const double MinAbsoluteScore = 0.01; // ignore near-zero noise

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
            var hybridResults = MergeResults(keywordResults, semanticResults, maxResults, query);

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
            var queryInfo = AnalyzeQuery(query);

            var keywordResults = allProducts
                .Select(p => new
                {
                    Product = p,
                    Score = CalculateKeywordScore(p.ProductName, queryInfo)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(maxResults)
                .Select(x => new ProductSearchResult
                {
                    Id = x.Product.Id,
                    ProductName = x.Product.ProductName,
                    ImageUrl = x.Product.ImageUrl,
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
                ImageUrl = string.IsNullOrWhiteSpace(vr.ImageUrl) ? ProductImageResolver.GetImageUrl(vr.ProductName) : vr.ImageUrl,
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

    private static double CalculateKeywordScore(string productName, QueryInfo queryInfo)
    {
        var nameLower = productName.ToLowerInvariant();
        var compactName = NormalizeForSearch(productName);
        double score = 0;

        // Exact match gets highest score
        if (nameLower.Equals(queryInfo.QueryLower, StringComparison.OrdinalIgnoreCase))
        {
            return 1.0;
        }

        // Contains full query
        if (nameLower.Contains(queryInfo.QueryLower))
        {
            score += 0.8;
        }

        // Check individual terms
        foreach (var term in queryInfo.Terms)
        {
            if (nameLower.Contains(term))
            {
                score += 0.3 / queryInfo.Terms.Length;
            }
        }

        // Starts with query bonus
        if (nameLower.StartsWith(queryInfo.QueryLower))
        {
            score += 0.1;
        }

        // Compact containment boost (handles punctuation/spacing differences)
        if (!string.IsNullOrWhiteSpace(queryInfo.CompactQuery) && compactName.Contains(queryInfo.CompactQuery))
        {
            score += 0.5;
        }

        // Model-like tokens are soft boosts via character n-gram overlap
        if (queryInfo.ModelNgrams.Count > 0)
        {
            var nameNgrams = BuildNgrams(compactName, 3);
            var overlap = CountOverlap(nameNgrams, queryInfo.ModelNgrams);
            if (overlap > 0)
            {
                var dice = (2.0 * overlap) / (nameNgrams.Count + queryInfo.ModelNgrams.Count);
                score += dice * 0.6;
            }
        }

        return Math.Min(score, 1.0);
    }

    private sealed record QueryInfo(
        string QueryLower,
        string CompactQuery,
        string[] Terms,
        HashSet<string> ModelNgrams);

    private static QueryInfo AnalyzeQuery(string query)
    {
        var queryLower = query.ToLowerInvariant();
        var terms = SplitTokens(queryLower).ToArray();
        var compactQuery = NormalizeForSearch(query);

        var modelTokens = terms
            .Where(IsModelLikeToken)
            .Select(NormalizeForSearch)
            .Where(t => t.Length >= 3)
            .Distinct();

        var modelNgrams = new HashSet<string>();
        foreach (var token in modelTokens)
        {
            foreach (var gram in BuildNgrams(token, 3))
            {
                modelNgrams.Add(gram);
            }
        }

        return new QueryInfo(queryLower, compactQuery, terms, modelNgrams);
    }

    private static bool IsModelLikeToken(string token)
    {
        if (token.Length < 5)
        {
            return false;
        }

        var hasLetter = token.Any(char.IsLetter);
        var hasDigit = token.Any(char.IsDigit);
        return hasLetter && hasDigit;
    }

    private static IEnumerable<string> SplitTokens(string value)
    {
        var token = new List<char>();
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                token.Add(ch);
            }
            else if (token.Count > 0)
            {
                yield return new string(token.ToArray());
                token.Clear();
            }
        }

        if (token.Count > 0)
        {
            yield return new string(token.ToArray());
        }
    }

    private static string NormalizeForSearch(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var chars = value.ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray();

        return new string(chars);
    }

    private static HashSet<string> BuildNgrams(string value, int n)
    {
        var grams = new HashSet<string>();
        if (string.IsNullOrWhiteSpace(value))
        {
            return grams;
        }

        if (value.Length <= n)
        {
            grams.Add(value);
            return grams;
        }

        for (var i = 0; i <= value.Length - n; i++)
        {
            grams.Add(value.Substring(i, n));
        }

        return grams;
    }

    private static int CountOverlap(HashSet<string> left, HashSet<string> right)
    {
        if (left.Count == 0 || right.Count == 0)
        {
            return 0;
        }

        var smaller = left.Count <= right.Count ? left : right;
        var larger = ReferenceEquals(smaller, left) ? right : left;

        var overlap = 0;
        foreach (var gram in smaller)
        {
            if (larger.Contains(gram))
            {
                overlap++;
            }
        }

        return overlap;
    }

    /// <summary>
    /// Merges keyword and semantic results using Reciprocal Rank Fusion (RRF)
    /// </summary>
    private IEnumerable<ProductSearchResult> MergeResults(
        IEnumerable<ProductSearchResult> keywordResults,
        IEnumerable<ProductSearchResult> semanticResults,
        int maxResults,
        string query)
    {
        const int k = 60; // RRF constant
        var queryLower = query.ToLowerInvariant();

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

                var nameLower = keywordMatch.Result.ProductName.ToLowerInvariant();
                if (nameLower.Equals(queryLower, StringComparison.OrdinalIgnoreCase))
                {
                    rrfScore += 1.0; // exact match should dominate
                }
                else if (nameLower.Contains(queryLower))
                {
                    rrfScore += 0.2; // strong keyword signal
                }
                else if (nameLower.StartsWith(queryLower))
                {
                    rrfScore += 0.1; // prefix boost
                }
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
        .ToList();

        if (fusedResults.Count == 0)
        {
            return Enumerable.Empty<ProductSearchResult>();
        }

        var topScore = fusedResults[0].RrfScore;
        var cutoff = Math.Max(topScore * MinRelativeToTop, MinAbsoluteScore);

        var filtered = fusedResults
            .Where(x => x.RrfScore >= cutoff)
            .Take(maxResults)
            .Select(x =>
            {
                x.Result.RelevanceScore = x.RrfScore;
                return x.Result;
            })
            .ToList();

        return filtered;
    }
}

