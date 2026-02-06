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
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
    public List<ProductInfo>? Data { get; set; }
}

public class ChatRequestModel
{
    public string Role { get; set; } = "user";
    public List<ChatContentItem> Contents { get; set; } = new();
}

public class ChatContentItem
{
    public string Text { get; set; } = string.Empty;
}

public enum ChatIntent
{
    ProductRequest = 0,

    Converse = 1
}
