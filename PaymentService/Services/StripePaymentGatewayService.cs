using PaymentService.Models;
using Stripe;
using Stripe.Checkout;

namespace PaymentService.Services;

public class StripePaymentGatewayService(
    IConfiguration configuration,
    IPaymentStore store,
    ILogger<StripePaymentGatewayService> logger) : IPaymentGatewayService
{
    public async Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(CheckoutSessionRequest request)
    {
        var paymentId = Guid.NewGuid().ToString("N");
        var stripeApiKey = configuration["Stripe:SecretKey"];
        var amountInCents = (long)Math.Round(request.Amount * 100m, MidpointRounding.AwayFromZero);

        if (string.IsNullOrWhiteSpace(stripeApiKey))
        {
            // Local mock mode if Stripe keys are not configured yet.
            var mockSessionId = $"mock_{Guid.NewGuid():N}";
            store.Add(new PaymentDto
            {
                PaymentId = paymentId,
                Provider = "stripe",
                ProviderSessionId = mockSessionId,
                Amount = request.Amount,
                CurrencyCode = request.CurrencyCode,
                OrderId = request.OrderId,
                UserId = request.UserId,
                Status = "PendingPayment"
            });

            return new CheckoutSessionResponse
            {
                PaymentId = paymentId,
                Provider = "stripe",
                ProviderSessionId = mockSessionId,
                CheckoutUrl = $"{request.SuccessUrl}?mock=true&paymentId={paymentId}",
                Status = "PendingPayment"
            };
        }

        StripeConfiguration.ApiKey = stripeApiKey;
        var service = new SessionService();

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = request.OrderId,
                ["paymentId"] = paymentId,
                ["userId"] = request.UserId ?? string.Empty
            },
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.CurrencyCode.ToLowerInvariant(),
                        UnitAmount = amountInCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Order {request.OrderId}"
                        }
                    }
                }
            ]
        };

        var createdSession = await service.CreateAsync(options);
        store.Add(new PaymentDto
        {
            PaymentId = paymentId,
            Provider = "stripe",
            ProviderSessionId = createdSession.Id,
            Amount = request.Amount,
            CurrencyCode = request.CurrencyCode,
            OrderId = request.OrderId,
            UserId = request.UserId,
            Status = "PendingPayment"
        });

        return new CheckoutSessionResponse
        {
            PaymentId = paymentId,
            Provider = "stripe",
            ProviderSessionId = createdSession.Id,
            CheckoutUrl = createdSession.Url ?? request.CancelUrl,
            Status = "PendingPayment"
        };
    }

    public Task HandleStripeWebhookAsync(string payload, string signatureHeader)
    {
        var endpointSecret = configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(endpointSecret))
        {
            logger.LogWarning("Stripe webhook secret is not configured. Ignoring webhook.");
            return Task.CompletedTask;
        }

        var stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, endpointSecret);
        if (!store.TryMarkEventProcessed(stripeEvent.Id))
        {
            return Task.CompletedTask;
        }

        if (string.Equals(stripeEvent.Type, "checkout.session.completed", StringComparison.OrdinalIgnoreCase))
        {
            var session = stripeEvent.Data.Object as Session;
            if (session != null)
            {
                var payment = store.GetByProviderSession(session.Id);
                if (payment != null)
                {
                    payment.Status = "Paid";
                    store.Update(payment);
                }
            }
        }
        else if (string.Equals(stripeEvent.Type, "checkout.session.expired", StringComparison.OrdinalIgnoreCase))
        {
            var session = stripeEvent.Data.Object as Session;
            if (session != null)
            {
                var payment = store.GetByProviderSession(session.Id);
                if (payment != null)
                {
                    payment.Status = "Expired";
                    store.Update(payment);
                }
            }
        }

        return Task.CompletedTask;
    }
}
