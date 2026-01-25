using ChatBotService.Models;
using Microsoft.Extensions.AI;

namespace ChatBotService.Services;

public interface IChatService
{
    Task<ChatResponse> ProcessMessageAsync(string message, List<ChatMessage> history);
    Task<ProductInfo?> GetProductDetailsAsync(int productId);
    Task<bool> CreateOrderAsync(int productId, int quantity);
}
