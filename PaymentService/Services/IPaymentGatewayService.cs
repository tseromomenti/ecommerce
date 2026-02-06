using PaymentService.Models;

namespace PaymentService.Services;

public interface IPaymentGatewayService
{
    Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(CheckoutSessionRequest request);
    Task HandleStripeWebhookAsync(string payload, string signatureHeader);
}
