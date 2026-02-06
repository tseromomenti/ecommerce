using PaymentService.Models;

namespace PaymentService.Services;

public interface IPaymentStore
{
    PaymentDto Add(PaymentDto payment);
    PaymentDto? Get(string paymentId);
    PaymentDto? GetByProviderSession(string providerSessionId);
    bool TryMarkEventProcessed(string eventId);
    void Update(PaymentDto payment);
}
