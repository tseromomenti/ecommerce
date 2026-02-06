using ChatBotService.Models;

namespace ChatBotService.Services;

public interface IChatService
{
    Task<ChatResponseModel> ProcessMessageAsync(string message, List<Microsoft.Extensions.AI.ChatMessage>? history);
    Task<ProductInfo?> GetProductDetailsAsync(int productId);
    Task<bool> CreateOrderAsync(int productId, int quantity);
    Task<string> CreateSessionAsync();
    Task<ChatMessageResponse> ProcessSessionMessageAsync(string sessionId, string message, string role = "user");
    Task<QuizStartResponse> StartQuizAsync(string sessionId, string quizType);
    Task<QuizAnswerResponse> AnswerQuizAsync(string sessionId, QuizAnswerRequest request);
}
