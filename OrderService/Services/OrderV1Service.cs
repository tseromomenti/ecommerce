using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using OrderService.Interfaces;
using OrderService.Models;

namespace OrderService.Services;

public class OrderV1Service(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<OrderV1Service> logger) : IOrderV1Service
{
    private static readonly ConcurrentDictionary<string, List<CartItemResponse>> Carts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, List<OrderV1Response>> OrdersByUser = new(StringComparer.OrdinalIgnoreCase);
    private readonly string paymentServiceUrl = configuration["Services:PaymentService"] ?? "http://localhost:5042";

    public Task<CartResponse> AddCartItemAsync(string userId, CartItemRequest request)
    {
        var cart = Carts.GetOrAdd(userId, _ => []);

        lock (cart)
        {
            var existing = cart.FirstOrDefault(i => i.ProductId == request.ProductId);
            if (existing != null)
            {
                existing.Quantity += request.Quantity;
                existing.UnitPrice = request.UnitPrice;
            }
            else
            {
                cart.Add(new CartItemResponse
                {
                    ItemId = Guid.NewGuid(),
                    ProductId = request.ProductId,
                    ProductName = request.ProductName,
                    UnitPrice = request.UnitPrice,
                    Quantity = request.Quantity
                });
            }
        }

        return GetCartAsync(userId);
    }

    public Task<CartResponse> GetCartAsync(string userId)
    {
        var items = Carts.GetOrAdd(userId, _ => []);
        var snapshot = items.Select(i => new CartItemResponse
        {
            ItemId = i.ItemId,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }).ToList();

        return Task.FromResult(new CartResponse
        {
            UserId = userId,
            Items = snapshot,
            Subtotal = snapshot.Sum(i => i.LineTotal),
            CurrencyCode = "USD"
        });
    }

    public Task<bool> RemoveCartItemAsync(string userId, Guid itemId)
    {
        if (!Carts.TryGetValue(userId, out var cart))
        {
            return Task.FromResult(false);
        }

        lock (cart)
        {
            var item = cart.FirstOrDefault(i => i.ItemId == itemId);
            if (item == null)
            {
                return Task.FromResult(false);
            }

            cart.Remove(item);
            return Task.FromResult(true);
        }
    }

    public async Task<OrderV1Response?> CheckoutAsync(string userId, CheckoutRequest request)
    {
        var cart = await GetCartAsync(userId);
        if (cart.Items.Count == 0)
        {
            return null;
        }

        var orderId = $"ord_{Guid.NewGuid():N}";
        var order = new OrderV1Response
        {
            OrderId = orderId,
            UserId = userId,
            Status = "PendingPayment",
            Items = cart.Items,
            Subtotal = cart.Subtotal,
            CurrencyCode = request.CurrencyCode
        };

        var checkoutResponse = await CreateCheckoutSessionAsync(order, request);
        if (checkoutResponse != null)
        {
            order.PaymentId = checkoutResponse.PaymentId;
            order.CheckoutUrl = checkoutResponse.CheckoutUrl;
        }

        var orders = OrdersByUser.GetOrAdd(userId, _ => []);
        lock (orders)
        {
            orders.Add(order);
        }

        Carts[userId] = [];
        return order;
    }

    public Task<IEnumerable<OrderV1Response>> GetOrdersAsync(string userId)
    {
        var orders = OrdersByUser.GetOrAdd(userId, _ => []);
        return Task.FromResult(orders.AsEnumerable());
    }

    public Task<OrderV1Response?> GetOrderAsync(string userId, string orderId)
    {
        var orders = OrdersByUser.GetOrAdd(userId, _ => []);
        return Task.FromResult(orders.FirstOrDefault(o => o.OrderId == orderId));
    }

    private async Task<CheckoutSessionResponse?> CreateCheckoutSessionAsync(OrderV1Response order, CheckoutRequest request)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            var payload = new
            {
                orderId = order.OrderId,
                amount = order.Subtotal,
                currencyCode = order.CurrencyCode,
                successUrl = request.SuccessUrl,
                cancelUrl = request.CancelUrl,
                userId = order.UserId
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authHeader))
            {
                client.DefaultRequestHeaders.Authorization = System.Net.Http.Headers.AuthenticationHeaderValue.Parse(authHeader);
            }
            var response = await client.PostAsync($"{paymentServiceUrl}/api/v1/payments/checkout-session", content);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Payment service returned non-success status {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CheckoutSessionResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create checkout session");
            return null;
        }
    }

    private sealed class CheckoutSessionResponse
    {
        public string PaymentId { get; set; } = string.Empty;
        public string CheckoutUrl { get; set; } = string.Empty;
    }
}
