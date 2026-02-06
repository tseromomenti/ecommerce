using System.Text.Json;
using ChatBotService.Models;
using Microsoft.Extensions.AI;

namespace ChatBotService.Services;

public class ChatService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IChatClient chatClient,
    ILogger<ChatService> logger) : IChatService
{
    private readonly string _inventoryServiceUrl = configuration["Services:InventoryService"] ?? "http://localhost:5001";
    private readonly string _orderServiceUrl = configuration["Services:OrderService"] ?? "http://localhost:5002";
    private readonly IChatClient chatClient = chatClient;

    public async Task<ChatResponseModel> ProcessMessageAsync(string message, List<ChatMessage>? history)
    {
        try
        {
            var messageText = message;
            var intentMessages = new List<ChatMessage>()
            {
                new ChatMessage(ChatRole.System, "Your role is to figure out if the user message is request for product search, or conversation request." +
                "If the input from the user likely item query request should return only 'product request', but if it looks like user is trying" +
                "to initiate conversation, asking question, or anything similar not related to search, return 'converse'."),
                new ChatMessage(ChatRole.User, messageText),
            };
            // classify intent
            var intent = await chatClient.GetResponseAsync<ChatIntent>(intentMessages);

            if (intent.Result.ToString().Equals("converse", StringComparison.OrdinalIgnoreCase))
            {
                var userMessage = new ChatMessage(ChatRole.User, messageText);
                return await this.SendMessageAsync(userMessage);
            }
            else if (intent.Result.ToString().Equals("productrequest", StringComparison.OrdinalIgnoreCase))
            {
                // Extract product keywords from natural language query
                var extractedQuery = await ExtractProductQueryAsync(messageText);
                logger.LogInformation("Extracted product query: '{ExtractedQuery}' from: '{Original}'", extractedQuery, messageText);
                
                var client = httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{_inventoryServiceUrl}/SearchProducts?query={Uri.EscapeDataString(extractedQuery)}");
                
                if (!response.IsSuccessStatusCode)
                {
                    return new ChatResponseModel
                    { 
                        Content = "Sorry, I couldn't search for products right now.",
                        Type = "text",
                        Role = "assistant"
                    };
                }

                var json = await response.Content.ReadAsStringAsync();
                var products = JsonSerializer.Deserialize<List<ProductInfo>>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (products == null || !products.Any())
                {
                    return new ChatResponseModel
                    {
                        Content = $"I couldn't find any products matching '{extractedQuery}'. Try different keywords.",
                        Type = "text",
                        Role = "assistant"
                    };
                }

                return new ChatResponseModel
                {
                    Content = $"I found {products.Count} product(s) matching '{extractedQuery}':",
                    Type = "products",
                    Data = products,
                    Role = "assistant"
                };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message: {Message}", message);
            return new ChatResponseModel
            {
                Content = "Sorry, something went wrong. Please try again.",
                Type = "text"
            };
        }

        return new ChatResponseModel { Content = "I'm not sure how to help with that.", Type = "text" };
    }

    public async Task<ProductInfo?> GetProductDetailsAsync(int productId)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_inventoryServiceUrl}/GetAllProducts");
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<ProductInfo>>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            return products?.FirstOrDefault(p => p.Id == productId);
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
                ProductName = product.ProductName,
                Quantity = quantity,
                Price = product.Price
            };

            var content = new StringContent(
                JsonSerializer.Serialize(orderData),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync($"{_orderServiceUrl}/api/order", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating order: ProductId={ProductId}, Quantity={Quantity}", productId, quantity);
            return false;
        }
    }

    public async Task<ChatIntent> ClassifyIntent(List<ChatMessage> messages)
    {
        var intent = await chatClient.GetResponseAsync<ChatIntent>(messages);

        return intent.Text == "converse" ? ChatIntent.Converse : ChatIntent.ProductRequest;
    }

    /// <summary>
    /// Extracts product search keywords from a natural language query.
    /// E.g., "I am looking for a warm winter jacket" -> "warm winter jacket"
    /// </summary>
    private async Task<string> ExtractProductQueryAsync(string naturalLanguageQuery)
    {
        var extractionMessages = new List<ChatMessage>()
        {
            new ChatMessage(ChatRole.System, 
                "Extract only the product search terms from the user's message. " +
                "Remove conversational words like 'I want', 'I'm looking for', 'can you find', 'please show me', etc. " +
                "Keep product attributes like color, size, brand, material. " +
                "Return ONLY the extracted product keywords, nothing else. " +
                "Examples:\n" +
                "- 'I am looking for a jacket' -> 'jacket'\n" +
                "- 'Can you find me a red Nike running shoes size 42' -> 'red Nike running shoes size 42'\n" +
                "- 'I need a warm winter coat under $100' -> 'warm winter coat'\n" +
                "- 'Show me wireless bluetooth headphones' -> 'wireless bluetooth headphones'"),
            new ChatMessage(ChatRole.User, naturalLanguageQuery)
        };

        var response = await chatClient.GetResponseAsync(extractionMessages);
        var assistantMessage = response.Messages.FirstOrDefault(m => m.Role == ChatRole.Assistant);
        
        // Get text from the assistant message contents
        string? extracted = null;
        if (assistantMessage?.Contents != null && assistantMessage.Contents.Count > 0)
        {
            var textContent = assistantMessage.Contents.FirstOrDefault(c => c is TextContent) as TextContent;
            extracted = textContent?.Text?.Trim();
        }
        
        // Fallback to original query if extraction fails
        return string.IsNullOrWhiteSpace(extracted) ? naturalLanguageQuery : extracted;
    }

    public async Task<ChatResponseModel> SendMessageAsync(ChatMessage message)
    {
        var assistantMessages = new List<ChatMessage>() 
        { 
            new ChatMessage(ChatRole.System, "You are helping the user in shopping"),
            message
        };
        var aiResponse = await chatClient.GetResponseAsync(assistantMessages);

        // Extract text from AI response
        var assistantMessage = aiResponse.Messages.FirstOrDefault(m => m.Role == ChatRole.Assistant);
        string text = "I'm here to help you shop!";
        if (assistantMessage?.Contents != null && assistantMessage.Contents.Count > 0)
        {
            var textContent = assistantMessage.Contents.FirstOrDefault(c => c is TextContent) as TextContent;
            text = textContent?.Text ?? text;
        }

        return new ChatResponseModel { Content = text, Type = "text", Role = "assistant" };
    }
}
