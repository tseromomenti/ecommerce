using Microsoft.AspNetCore.Mvc;
using ChatBotService.Models;
using ChatBotService.Services;
using AIChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatResponseModel = ChatBotService.Models.ChatResponseModel;

namespace ChatBotService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController(IChatService chatService) : ControllerBase
{
    [HttpPost("message")]
    public async Task<ActionResult<ChatResponseModel>> SendMessage([FromBody] AIChatMessage message)
    {
        var lastMessage = message.Contents[^1].ToString();
        if (string.IsNullOrWhiteSpace(lastMessage))
        {
            return BadRequest("Message cannot be empty");
        }

        var response = await chatService.ProcessMessageAsync(lastMessage, null);
        return Ok(response);
    }
}

public class OrderRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
