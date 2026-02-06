using ChatBotService.Models;
using ChatBotService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatBotService.Controllers;

[ApiController]
[Route("api")]
public class ChatController(IChatService chatService) : ControllerBase
{
    // Legacy endpoint kept for backward compatibility.
    [HttpPost("chat/message")]
    [AllowAnonymous]
    public async Task<ActionResult<ChatResponseModel>> SendMessage([FromBody] ChatRequestModel request)
    {
        var lastMessage = request.ResolveContent();
        if (string.IsNullOrWhiteSpace(lastMessage))
        {
            return BadRequest("Message cannot be empty");
        }

        var response = await chatService.ProcessMessageAsync(lastMessage, null);
        return Ok(response);
    }

    // Legacy endpoint kept for backward compatibility.
    [HttpGet("chat/product/{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductInfo>> GetProduct(int id)
    {
        var product = await chatService.GetProductDetailsAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    // Legacy endpoint kept for backward compatibility.
    [HttpPost("chat/order")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateOrder([FromBody] LegacyOrderRequest request)
    {
        var success = await chatService.CreateOrderAsync(request.ProductId, request.Quantity);
        if (!success)
        {
            return BadRequest(new { message = "Failed to create order." });
        }

        return Ok(new { message = "Order created." });
    }

    [HttpPost("v1/chat/sessions")]
    [Authorize]
    public async Task<IActionResult> CreateSession([FromBody] ChatSessionCreateRequest? _)
    {
        var sessionId = await chatService.CreateSessionAsync();
        return Ok(new { sessionId });
    }

    [HttpPost("v1/chat/sessions/{sessionId}/messages")]
    [Authorize]
    public async Task<ActionResult<ChatMessageResponse>> SendSessionMessage(string sessionId, [FromBody] ChatMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "Message content cannot be empty." });
        }

        var response = await chatService.ProcessSessionMessageAsync(sessionId, request.Content);
        return Ok(response);
    }

    [HttpPost("v1/chat/sessions/{sessionId}/quiz/start")]
    [Authorize]
    public async Task<ActionResult<QuizStartResponse>> StartQuiz(string sessionId, [FromQuery] string quizType = "anime")
    {
        var response = await chatService.StartQuizAsync(sessionId, quizType);
        return Ok(response);
    }

    [HttpPost("v1/chat/sessions/{sessionId}/quiz/answer")]
    [Authorize]
    public async Task<ActionResult<QuizAnswerResponse>> AnswerQuiz(string sessionId, [FromBody] QuizAnswerRequest request)
    {
        var response = await chatService.AnswerQuizAsync(sessionId, request);
        return Ok(response);
    }
}

public class LegacyOrderRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
