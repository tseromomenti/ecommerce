using Microsoft.AspNetCore.Mvc;
using ChatBotService.Models;
using ChatBotService.Services;

namespace ChatBotService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController(IChatService chatService) : ControllerBase
{
    [HttpPost("message")]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message cannot be empty");
        }

        var response = await chatService.ProcessMessageAsync(request.Message, request.History);
        return Ok(response);
    }

    [HttpGet("product/{productId}")]
    public async Task<ActionResult<ProductInfo>> GetProductDetails(int productId)
    {
        var product = await chatService.GetProductDetailsAsync(productId);
        if (product == null)
        {
            return NotFound();
        }
        return Ok(product);
    }

    [HttpPost("order")]
    public async Task<ActionResult> CreateOrder([FromBody] OrderRequest request)
    {
        var success = await chatService.CreateOrderAsync(request.ProductId, request.Quantity);
        if (!success)
        {
            return BadRequest("Unable to process order");
        }
        return Ok(new { message = "Order created successfully" });
    }
}

public class OrderRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
