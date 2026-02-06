using System.Text;
using System.Text.Json;
using ChatBotService.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace ChatBotService.Services;

public class ChatService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IChatClient chatClient,
    IMemoryCache memoryCache,
    ILogger<ChatService> logger) : IChatService
{
    private readonly string inventoryServiceUrl = configuration["Services:InventoryService"] ?? "http://localhost:5068";
    private readonly string orderServiceUrl = configuration["Services:OrderService"] ?? "http://localhost:5123";
    private readonly IChatClient chatClient = chatClient;
    private readonly TimeSpan sessionTtl = TimeSpan.FromHours(8);

    public async Task<ChatResponseModel> ProcessMessageAsync(string message, List<ChatMessage>? history)
    {
        var sessionId = $"legacy-{Guid.NewGuid():N}";
        var sessionResponse = await ProcessSessionMessageAsync(sessionId, message, "user");
        return new ChatResponseModel
        {
            Content = sessionResponse.Content,
            Role = sessionResponse.Role,
            Type = sessionResponse.Type == "clarification" ? "text" : sessionResponse.Type,
            Data = sessionResponse.Products
        };
    }

    public async Task<ProductInfo?> GetProductDetailsAsync(int productId)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{inventoryServiceUrl}/GetProductById?productId={productId}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ProductInfo>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting product details: {ProductId}", productId);
            return null;
        }
    }

    public async Task<bool> CreateOrderAsync(int productId, int quantity)
    {
        try
        {
            var product = await GetProductDetailsAsync(productId);
            if (product == null)
            {
                return false;
            }

            var client = httpClientFactory.CreateClient();
            var orderData = new
            {
                productName = product.ProductName,
                quantity
            };

            var content = new StringContent(JsonSerializer.Serialize(orderData), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{orderServiceUrl}/api/order", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating order: ProductId={ProductId}, Quantity={Quantity}", productId, quantity);
            return false;
        }
    }

    public Task<string> CreateSessionAsync()
    {
        var sessionId = Guid.NewGuid().ToString("N");
        GetOrCreateSession(sessionId);
        return Task.FromResult(sessionId);
    }

    public async Task<ChatMessageResponse> ProcessSessionMessageAsync(string sessionId, string message, string role = "user")
    {
        var session = GetOrCreateSession(sessionId);
        session.History.Add((role, message));

        try
        {
            var intent = InferIntent(message);
            if (intent == "smalltalk")
            {
                var reply = await SendMessageAsync(new ChatMessage(ChatRole.User, message));
                return new ChatMessageResponse
                {
                    SessionId = sessionId,
                    Content = reply.Content,
                    Role = "assistant",
                    Type = "text",
                    AppliedFilters = session.Filters,
                    NextAction = "none"
                };
            }

            var extractedQuery = await ExtractProductQueryAsync(message);
            var extractedFilters = ExtractFilters(message);
            session.Filters = MergeFilters(session.Filters, extractedFilters);

            var products = await SearchProductsAsync(extractedQuery, session.Filters, 8);
            if (products.Count == 0)
            {
                return new ChatMessageResponse
                {
                    SessionId = sessionId,
                    Content = $"I couldn't find products for '{extractedQuery}'. Try changing category, budget, or size.",
                    Role = "assistant",
                    Type = "text",
                    AppliedFilters = session.Filters,
                    SuggestedReplies = ["Show me warm clothes under $80", "Try grocery snacks under $20", "Show in-stock electronics"],
                    NextAction = "refine_search"
                };
            }

            session.LastProducts = products.Select(p => p.Id).ToList();
            var shouldClarify = ShouldClarify(products, session.Filters);
            if (shouldClarify.ShouldAsk)
            {
                return new ChatMessageResponse
                {
                    SessionId = sessionId,
                    Content = "I found some options and can narrow them down further.",
                    Role = "assistant",
                    Type = "clarification",
                    Products = products.Take(6).ToList(),
                    ClarifyingQuestion = shouldClarify.Question,
                    SuggestedReplies = shouldClarify.Suggestions,
                    AppliedFilters = session.Filters,
                    NextAction = "clarify"
                };
            }

            return new ChatMessageResponse
            {
                SessionId = sessionId,
                Content = $"Found {products.Count} great options. Tell me if you want a tighter budget, specific brand, size, or category.",
                Role = "assistant",
                Type = "products",
                Products = products,
                AppliedFilters = session.Filters,
                SuggestedReplies = ["Only in-stock", "Under $50", "Show similar options"],
                NextAction = "show_results"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing session message for session {SessionId}", sessionId);
            return new ChatMessageResponse
            {
                SessionId = sessionId,
                Content = "I hit an error while searching. Please try again with a shorter query.",
                Role = "assistant",
                Type = "text",
                AppliedFilters = session.Filters,
                NextAction = "none"
            };
        }
    }

    public Task<QuizStartResponse> StartQuizAsync(string sessionId, string quizType)
    {
        var session = GetOrCreateSession(sessionId);
        var definition = QuizCatalog.GetDefinition(quizType);

        session.ActiveQuiz = new SessionQuizState
        {
            QuizType = definition.QuizType,
            QuestionIndex = 0,
            Scores = new Dictionary<string, int>()
        };

        return Task.FromResult(new QuizStartResponse
        {
            SessionId = sessionId,
            QuizType = definition.QuizType,
            Question = definition.Questions[0]
        });
    }

    public async Task<QuizAnswerResponse> AnswerQuizAsync(string sessionId, QuizAnswerRequest request)
    {
        var session = GetOrCreateSession(sessionId);
        if (session.ActiveQuiz == null)
        {
            return new QuizAnswerResponse { Completed = false };
        }

        var definition = QuizCatalog.GetDefinition(session.ActiveQuiz.QuizType);
        if (session.ActiveQuiz.QuestionIndex >= definition.Questions.Count)
        {
            return new QuizAnswerResponse { Completed = true };
        }

        var currentQuestion = definition.Questions[session.ActiveQuiz.QuestionIndex];
        if (!string.Equals(currentQuestion.Id, request.QuestionId, StringComparison.OrdinalIgnoreCase))
        {
            return new QuizAnswerResponse { Completed = false, NextQuestion = currentQuestion };
        }

        if (definition.OptionScores.TryGetValue(request.AnswerKey, out var score))
        {
            if (!session.ActiveQuiz.Scores.TryAdd(score.PersonaKey, score.Weight))
            {
                session.ActiveQuiz.Scores[score.PersonaKey] += score.Weight;
            }
        }

        session.ActiveQuiz.QuestionIndex++;
        if (session.ActiveQuiz.QuestionIndex < definition.Questions.Count)
        {
            return new QuizAnswerResponse
            {
                Completed = false,
                NextQuestion = definition.Questions[session.ActiveQuiz.QuestionIndex]
            };
        }

        var winnerKey = session.ActiveQuiz.Scores
            .OrderByDescending(x => x.Value)
            .Select(x => x.Key)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(winnerKey))
        {
            winnerKey = definition.Personas.Keys.First();
        }

        var winnerScore = session.ActiveQuiz.Scores.TryGetValue(winnerKey, out var winnerNumericScore) ? winnerNumericScore : 1;
        var persona = definition.Personas.GetValueOrDefault(winnerKey)!;
        var confidence = session.ActiveQuiz.Scores.Count == 0 ? 0.0 : winnerScore / (double)session.ActiveQuiz.Scores.Values.Sum();
        var recommendedProducts = await SearchProductsAsync(string.Join(' ', persona.RecommendedTags), new SearchFilters(), 6);

        session.Filters.PersonaTags = persona.RecommendedTags;
        session.ActiveQuiz = null;

        return new QuizAnswerResponse
        {
            Completed = true,
            Result = new QuizResult
            {
                QuizType = definition.QuizType,
                PersonaKey = persona.Key,
                PersonaLabel = persona.Label,
                Confidence = Math.Round(confidence, 2),
                RecommendedTags = persona.RecommendedTags
            },
            RecommendedProducts = recommendedProducts
        };
    }

    private ChatSessionState GetOrCreateSession(string sessionId)
    {
        return memoryCache.GetOrCreate(sessionId, entry =>
        {
            entry.SlidingExpiration = sessionTtl;
            return new ChatSessionState();
        })!;
    }

    private static string InferIntent(string message)
    {
        var text = message.ToLowerInvariant();
        if (text.Contains("hello") || text.Contains("how are you") || text.Contains("joke"))
        {
            return "smalltalk";
        }

        if (text.Contains("quiz"))
        {
            return "quiz";
        }

        return "product_search";
    }

    private static SearchFilters ExtractFilters(string message)
    {
        var lower = message.ToLowerInvariant();
        var filters = new SearchFilters();

        if (lower.Contains("in stock"))
        {
            filters.InStockOnly = true;
        }

        var budgetMatch = System.Text.RegularExpressions.Regex.Match(lower, @"under\s*\$?(\d+)");
        if (budgetMatch.Success && decimal.TryParse(budgetMatch.Groups[1].Value, out var budget))
        {
            filters.MaxPrice = budget;
        }

        if (lower.Contains("clothes") || lower.Contains("jacket") || lower.Contains("hoodie"))
        {
            filters.Category = "clothing";
        }
        else if (lower.Contains("grocery") || lower.Contains("snack") || lower.Contains("food"))
        {
            filters.Category = "grocery";
        }
        else if (lower.Contains("electronics") || lower.Contains("keyboard") || lower.Contains("mouse"))
        {
            filters.Category = "electronics";
        }

        var brands = new[] { "nike", "adidas", "sony", "apple", "samsung", "anker", "logitech" };
        filters.Brand = brands.FirstOrDefault(lower.Contains);

        var sizeMatch = System.Text.RegularExpressions.Regex.Match(lower, @"\b(xs|s|m|l|xl|xxl|\d{2})\b");
        if (sizeMatch.Success)
        {
            filters.Size = sizeMatch.Groups[1].Value.ToUpperInvariant();
        }

        var colors = new[] { "black", "white", "red", "blue", "green", "yellow", "gray", "brown" };
        filters.Color = colors.FirstOrDefault(lower.Contains);
        return filters;
    }

    private static SearchFilters MergeFilters(SearchFilters existing, SearchFilters incoming)
    {
        return new SearchFilters
        {
            Category = incoming.Category ?? existing.Category,
            Subcategory = incoming.Subcategory ?? existing.Subcategory,
            Color = incoming.Color ?? existing.Color,
            Size = incoming.Size ?? existing.Size,
            Brand = incoming.Brand ?? existing.Brand,
            MinPrice = incoming.MinPrice ?? existing.MinPrice,
            MaxPrice = incoming.MaxPrice ?? existing.MaxPrice,
            InStockOnly = existing.InStockOnly || incoming.InStockOnly,
            PersonaTags = incoming.PersonaTags.Count > 0 ? incoming.PersonaTags : existing.PersonaTags
        };
    }

    private static ClarificationDecision ShouldClarify(List<ProductInfo> products, SearchFilters filters)
    {
        if (products.Count == 0)
        {
            return ClarificationDecision.None;
        }

        var top = products.Max(p => p.RelevanceScore);
        var bottom = products.Min(p => p.RelevanceScore);
        var spread = top - bottom;

        if (products.Count > 6 || spread < 0.15)
        {
            if (string.IsNullOrWhiteSpace(filters.Category))
            {
                return new ClarificationDecision(
                    true,
                    "Do you want clothing, groceries, household, or electronics?",
                    ["Clothing", "Groceries", "Household", "Electronics"]);
            }

            if (filters.MaxPrice is null)
            {
                return new ClarificationDecision(true, "What budget should I stay under?", ["Under $25", "Under $50", "Under $100"]);
            }

            if (string.IsNullOrWhiteSpace(filters.Size))
            {
                return new ClarificationDecision(true, "Do you have a preferred size?", ["S", "M", "L", "XL"]);
            }

            if (string.IsNullOrWhiteSpace(filters.Brand))
            {
                return new ClarificationDecision(true, "Any preferred brand?", ["Nike", "Adidas", "Sony", "Logitech"]);
            }
        }

        return ClarificationDecision.None;
    }

    private async Task<List<ProductInfo>> SearchProductsAsync(string query, SearchFilters filters, int maxResults)
    {
        var client = httpClientFactory.CreateClient();

        try
        {
            var request = new InventorySearchRequest
            {
                Query = query,
                MaxResults = maxResults,
                Filters = filters
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{inventoryServiceUrl}/api/v1/inventory/search/hybrid", content);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var products = JsonSerializer.Deserialize<List<ProductInfo>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (products != null && products.Count > 0)
                {
                    return products;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "V1 inventory search failed, falling back to legacy endpoint.");
        }

        try
        {
            var response = await client.GetAsync($"{inventoryServiceUrl}/SearchProducts?query={Uri.EscapeDataString(query)}&maxResults={maxResults}");
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var json = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<ProductInfo>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return products ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Legacy inventory search failed.");
            return [];
        }
    }

    private async Task<string> ExtractProductQueryAsync(string naturalLanguageQuery)
    {
        try
        {
            var extractionMessages = new List<ChatMessage>()
            {
                new(ChatRole.System,
                    "Extract concise product search terms from user text. Return plain keywords only."),
                new(ChatRole.User, naturalLanguageQuery)
            };

            var response = await chatClient.GetResponseAsync(extractionMessages);
            var assistantMessage = response.Messages.FirstOrDefault(m => m.Role == ChatRole.Assistant);
            if (assistantMessage?.Contents != null && assistantMessage.Contents.Count > 0)
            {
                var textContent = assistantMessage.Contents.OfType<TextContent>().FirstOrDefault();
                var extracted = textContent?.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(extracted))
                {
                    return extracted;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to extract query with LLM. Falling back to user message.");
        }

        return naturalLanguageQuery;
    }

    private async Task<ChatResponseModel> SendMessageAsync(ChatMessage message)
    {
        var assistantMessages = new List<ChatMessage>()
        {
            new(ChatRole.System, "You are helping the user in shopping."),
            message
        };
        var aiResponse = await chatClient.GetResponseAsync(assistantMessages);

        var assistantMessage = aiResponse.Messages.FirstOrDefault(m => m.Role == ChatRole.Assistant);
        var text = "I'm here to help you shop.";
        if (assistantMessage?.Contents != null && assistantMessage.Contents.Count > 0)
        {
            var textContent = assistantMessage.Contents.OfType<TextContent>().FirstOrDefault();
            text = textContent?.Text ?? text;
        }

        return new ChatResponseModel { Content = text, Type = "text", Role = "assistant" };
    }

    private sealed class ChatSessionState
    {
        public SearchFilters Filters { get; set; } = new();
        public List<(string Role, string Content)> History { get; } = new();
        public List<int> LastProducts { get; set; } = new();
        public SessionQuizState? ActiveQuiz { get; set; }
    }

    private sealed class SessionQuizState
    {
        public string QuizType { get; set; } = string.Empty;
        public int QuestionIndex { get; set; }
        public Dictionary<string, int> Scores { get; set; } = new();
    }

    private sealed record InventorySearchRequest
    {
        public string Query { get; init; } = string.Empty;
        public int MaxResults { get; init; } = 8;
        public SearchFilters Filters { get; init; } = new();
    }

    private sealed record ClarificationDecision(bool ShouldAsk, string Question, List<string> Suggestions)
    {
        public static ClarificationDecision None => new(false, string.Empty, []);
    }
}

internal static class QuizCatalog
{
    private static readonly Dictionary<string, QuizDefinition> Catalog = new(StringComparer.OrdinalIgnoreCase)
    {
        ["anime"] = new QuizDefinition
        {
            QuizType = "anime",
            Questions =
            [
                new QuizQuestion
                {
                    Id = "q1",
                    Prompt = "Pick your weekend vibe:",
                    Options =
                    [
                        new QuizQuestionOption { Key = "anime_q1_adventure", Label = "Adventure outdoors" },
                        new QuizQuestionOption { Key = "anime_q1_strategy", Label = "Plan and optimize everything" },
                        new QuizQuestionOption { Key = "anime_q1_style", Label = "Dress sharp and meet friends" }
                    ]
                },
                new QuizQuestion
                {
                    Id = "q2",
                    Prompt = "Choose your go-to purchase:",
                    Options =
                    [
                        new QuizQuestionOption { Key = "anime_q2_practical", Label = "Practical essentials" },
                        new QuizQuestionOption { Key = "anime_q2_performance", Label = "Performance gear" },
                        new QuizQuestionOption { Key = "anime_q2_statement", Label = "Bold statement items" }
                    ]
                }
            ],
            OptionScores = new Dictionary<string, PersonaScore>(StringComparer.OrdinalIgnoreCase)
            {
                ["anime_q1_adventure"] = new("joseph_type", 3),
                ["anime_q1_strategy"] = new("kakyoin_type", 3),
                ["anime_q1_style"] = new("dio_type", 3),
                ["anime_q2_practical"] = new("joseph_type", 2),
                ["anime_q2_performance"] = new("kakyoin_type", 2),
                ["anime_q2_statement"] = new("dio_type", 2)
            },
            Personas = new Dictionary<string, Persona>(StringComparer.OrdinalIgnoreCase)
            {
                ["joseph_type"] = new("joseph_type", "Joseph-style hustler", ["outdoor", "jacket", "sneakers", "travel"]),
                ["kakyoin_type"] = new("kakyoin_type", "Precision strategist", ["electronics", "keyboard", "desk", "smart"]),
                ["dio_type"] = new("dio_type", "Bold trend setter", ["premium", "fashion", "fragrance", "luxury"])
            }
        },
        ["food"] = new QuizDefinition
        {
            QuizType = "food",
            Questions =
            [
                new QuizQuestion
                {
                    Id = "q1",
                    Prompt = "Pick a flavor profile:",
                    Options =
                    [
                        new QuizQuestionOption { Key = "food_q1_spicy", Label = "Spicy and bold" },
                        new QuizQuestionOption { Key = "food_q1_sweet", Label = "Sweet and cozy" },
                        new QuizQuestionOption { Key = "food_q1_fresh", Label = "Fresh and light" }
                    ]
                },
                new QuizQuestion
                {
                    Id = "q2",
                    Prompt = "Choose a snack moment:",
                    Options =
                    [
                        new QuizQuestionOption { Key = "food_q2_movie", Label = "Movie night" },
                        new QuizQuestionOption { Key = "food_q2_work", Label = "Work break" },
                        new QuizQuestionOption { Key = "food_q2_fitness", Label = "After workout" }
                    ]
                }
            ],
            OptionScores = new Dictionary<string, PersonaScore>(StringComparer.OrdinalIgnoreCase)
            {
                ["food_q1_spicy"] = new("spice_hunter", 3),
                ["food_q1_sweet"] = new("comfort_lover", 3),
                ["food_q1_fresh"] = new("clean_eater", 3),
                ["food_q2_movie"] = new("comfort_lover", 2),
                ["food_q2_work"] = new("spice_hunter", 2),
                ["food_q2_fitness"] = new("clean_eater", 2)
            },
            Personas = new Dictionary<string, Persona>(StringComparer.OrdinalIgnoreCase)
            {
                ["spice_hunter"] = new("spice_hunter", "Spice hunter", ["snacks", "sauce", "chips", "instant"]),
                ["comfort_lover"] = new("comfort_lover", "Comfort food fan", ["chocolate", "cookies", "tea", "blanket"]),
                ["clean_eater"] = new("clean_eater", "Clean energy eater", ["protein", "nuts", "organic", "water"])
            }
        }
    };

    public static QuizDefinition GetDefinition(string quizType)
    {
        if (Catalog.TryGetValue(quizType, out var definition))
        {
            return definition;
        }

        return Catalog["anime"];
    }

    internal sealed class QuizDefinition
    {
        public string QuizType { get; set; } = string.Empty;
        public List<QuizQuestion> Questions { get; set; } = new();
        public Dictionary<string, PersonaScore> OptionScores { get; set; } = new();
        public Dictionary<string, Persona> Personas { get; set; } = new();
    }

    internal sealed record PersonaScore(string PersonaKey, int Weight);
    internal sealed record Persona(string Key, string Label, List<string> RecommendedTags);
}
