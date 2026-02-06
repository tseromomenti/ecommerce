namespace ChatBotService.Models;

public class ProductInfo
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int AvailableStock { get; set; }
    public double RelevanceScore { get; set; }
}

public class ChatResponseModel
{
    public string Content { get; set; } = string.Empty;
    public string Role { get; set; } = "assistant";
    public string Type { get; set; } = "text";
    public List<ProductInfo>? Data { get; set; }

    // Backward compatibility for older clients expecting `message`.
    public string Message => Content;
}

public class ChatRequestModel
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public List<ChatContentItem> Contents { get; set; } = new();

    public string ResolveContent()
    {
        if (!string.IsNullOrWhiteSpace(Content))
        {
            return Content.Trim();
        }

        var parts = Contents
            .Select(c => c.Text?.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArray();

        return parts.Length == 0 ? string.Empty : string.Join(" ", parts!);
    }
}

public class ChatContentItem
{
    public string Text { get; set; } = string.Empty;
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
    public List<string> PersonaTags { get; set; } = new();
}

public class ChatSessionCreateRequest
{
    public string? SessionName { get; set; }
}

public class ChatMessageRequest
{
    public string Content { get; set; } = string.Empty;
}

public class ChatMessageResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Role { get; set; } = "assistant";
    public string Type { get; set; } = "text";
    public List<ProductInfo> Products { get; set; } = new();
    public string? ClarifyingQuestion { get; set; }
    public List<string> SuggestedReplies { get; set; } = new();
    public SearchFilters AppliedFilters { get; set; } = new();
    public string NextAction { get; set; } = "none";
}

public class QuizQuestionOption
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class QuizQuestion
{
    public string Id { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public List<QuizQuestionOption> Options { get; set; } = new();
}

public class QuizStartResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string QuizType { get; set; } = string.Empty;
    public QuizQuestion? Question { get; set; }
}

public class QuizAnswerRequest
{
    public string QuestionId { get; set; } = string.Empty;
    public string AnswerKey { get; set; } = string.Empty;
}

public class QuizResult
{
    public string QuizType { get; set; } = string.Empty;
    public string PersonaKey { get; set; } = string.Empty;
    public string PersonaLabel { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public List<string> RecommendedTags { get; set; } = new();
}

public class QuizAnswerResponse
{
    public bool Completed { get; set; }
    public QuizQuestion? NextQuestion { get; set; }
    public QuizResult? Result { get; set; }
    public List<ProductInfo> RecommendedProducts { get; set; } = new();
}

public enum ChatIntent
{
    ProductRequest = 0,

    Converse = 1
}
