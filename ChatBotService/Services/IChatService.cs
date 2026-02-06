using ChatBotService.Models;
using AIChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace ChatBotService.Services;

public interface IChatService
{
    Task<ChatResponseModel> ProcessMessageAsync(string message, List<AIChatMessage>? history);
    Task<ProductInfo?> GetProductDetailsAsync(int productId);
    Task<bool> CreateOrderAsync(int productId, int quantity);
}
