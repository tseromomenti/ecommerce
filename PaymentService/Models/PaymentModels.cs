namespace PaymentService.Models;

public class CheckoutSessionRequest
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string SuccessUrl { get; set; } = "http://localhost:4200/checkout/success";
    public string CancelUrl { get; set; } = "http://localhost:4200/checkout/cancel";
    public string? UserId { get; set; }
}

public class CheckoutSessionResponse
{
    public string PaymentId { get; set; } = string.Empty;
    public string Provider { get; set; } = "stripe";
    public string ProviderSessionId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "PendingPayment";
}

public class PaymentDto
{
    public string PaymentId { get; set; } = string.Empty;
    public string Provider { get; set; } = "stripe";
    public string ProviderSessionId { get; set; } = string.Empty;
    public string Status { get; set; } = "PendingPayment";
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string OrderId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
