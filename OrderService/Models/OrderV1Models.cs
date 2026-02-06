namespace OrderService.Models;

public class CartItemRequest
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public class CartItemResponse
{
    public Guid ItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public class CartResponse
{
    public string UserId { get; set; } = string.Empty;
    public List<CartItemResponse> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public string CurrencyCode { get; set; } = "USD";
}

public class CheckoutRequest
{
    public string CurrencyCode { get; set; } = "USD";
    public string SuccessUrl { get; set; } = "http://localhost:4200/checkout/success";
    public string CancelUrl { get; set; } = "http://localhost:4200/checkout/cancel";
}

public class OrderV1Response
{
    public string OrderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Status { get; set; } = "PendingPayment";
    public List<CartItemResponse> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? PaymentId { get; set; }
    public string? CheckoutUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
