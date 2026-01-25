using Microsoft.AspNetCore.Mvc;
using ChatBotService.Models;
using ChatBotService.Services;
using Microsoft.Extensions.AI;
using OllamaSharp.Models.Chat;

namespace ChatBotService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController(IChatService chatService) : ControllerBase
{
    [HttpPost("message")]
    public async Task<ActionResult<ChatMessageModel>> SendMessage([FromBody] ChatMessageModel message)
    {
        var lastMessage = message.Content;
        if (string.IsNullOrWhiteSpace(lastMessage))
        {
            return BadRequest("Message cannot be empty");
        }

        var response = await chatService.ProcessMessageAsync(lastMessage, null);

        var responseMessage = new ChatMessageModel
        {
            Content = response.Text,
            Role = "assistant"
        };

        return Ok(responseMessage);
    }
}

public class OrderRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
