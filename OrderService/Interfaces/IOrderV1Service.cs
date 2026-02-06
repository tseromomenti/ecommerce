using OrderService.Models;

namespace OrderService.Interfaces;

public interface IOrderV1Service
{
    Task<CartResponse> AddCartItemAsync(string userId, CartItemRequest request);
    Task<CartResponse> GetCartAsync(string userId);
    Task<bool> RemoveCartItemAsync(string userId, Guid itemId);
    Task<OrderV1Response?> CheckoutAsync(string userId, CheckoutRequest request);
    Task<IEnumerable<OrderV1Response>> GetOrdersAsync(string userId);
    Task<OrderV1Response?> GetOrderAsync(string userId, string orderId);
}
