using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Interfaces;
using OrderService.Models;

namespace OrderService.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class OrdersV1Controller(IOrderV1Service orderV1Service) : ControllerBase
{
    [HttpPost("cart/items")]
    public async Task<ActionResult<CartResponse>> AddCartItem([FromBody] CartItemRequest request)
    {
        if (request.Quantity <= 0 || request.UnitPrice < 0 || request.ProductId <= 0)
        {
            return BadRequest(new { message = "Invalid cart item payload." });
        }

        var userId = GetUserId();
        var cart = await orderV1Service.AddCartItemAsync(userId, request);
        return Ok(cart);
    }

    [HttpGet("cart")]
    public async Task<ActionResult<CartResponse>> GetCart()
    {
        var userId = GetUserId();
        var cart = await orderV1Service.GetCartAsync(userId);
        return Ok(cart);
    }

    [HttpDelete("cart/items/{itemId:guid}")]
    public async Task<IActionResult> RemoveCartItem(Guid itemId)
    {
        var userId = GetUserId();
        var removed = await orderV1Service.RemoveCartItemAsync(userId, itemId);
        return removed ? NoContent() : NotFound();
    }

    [HttpPost("orders/checkout")]
    public async Task<ActionResult<OrderV1Response>> Checkout([FromBody] CheckoutRequest request)
    {
        var userId = GetUserId();
        var order = await orderV1Service.CheckoutAsync(userId, request);
        if (order == null)
        {
            return BadRequest(new { message = "Cart is empty." });
        }

        return Ok(order);
    }

    [HttpGet("orders/me")]
    public async Task<ActionResult<IEnumerable<OrderV1Response>>> GetMyOrders()
    {
        var userId = GetUserId();
        var orders = await orderV1Service.GetOrdersAsync(userId);
        return Ok(orders);
    }

    [HttpGet("orders/{orderId}")]
    public async Task<ActionResult<OrderV1Response>> GetOrder(string orderId)
    {
        var userId = GetUserId();
        var order = await orderV1Service.GetOrderAsync(userId, orderId);
        if (order == null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    private string GetUserId()
    {
        return User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? "anonymous-user";
    }
}
