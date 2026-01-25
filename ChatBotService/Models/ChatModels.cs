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

public class ChatMessageModel
{
    public string Content { get; set; }
    public string Role { get; set; }
}

public enum ChatIntent
{
    ProductRequest = 0,

    Converse = 1
}
