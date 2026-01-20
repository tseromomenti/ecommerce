using System.Text.Json;
using ChatBotService.Models;

namespace ChatBotService.Services;

public class ChatService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ChatService> logger) : IChatService
{
    private readonly string _inventoryServiceUrl = configuration["Services:InventoryService"] ?? "http://localhost:5001";
    private readonly string _orderServiceUrl = configuration["Services:OrderService"] ?? "http://localhost:5002";

    public async Task<ChatResponse> ProcessMessageAsync(string message, List<ChatMessage> history)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_inventoryServiceUrl}/SearchProducts?query={Uri.EscapeDataString(message)}");
            
            if (!response.IsSuccessStatusCode)
            {
                return new ChatResponse 
                { 
                    Message = "Sorry, I couldn't search for products right now.",
                    Type = "text"
                };
            }

            var json = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<ProductInfo>>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (products == null || !products.Any())
            {
                return new ChatResponse
                {
                    Message = $"I couldn't find any products matching '{message}'. Try different keywords.",
                    Type = "text"
                };
            }

            return new ChatResponse
            {
                Message = $"I found {products.Count} product(s) matching '{message}':",
                Type = "products",
                Data = products
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message: {Message}", message);
            return new ChatResponse
            {
                Message = "Sorry, something went wrong. Please try again.",
                Type = "text"
            };
        }
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
}
