using System.Collections.Concurrent;
using PaymentService.Models;

namespace PaymentService.Services;

public class InMemoryPaymentStore : IPaymentStore
{
    private readonly ConcurrentDictionary<string, PaymentDto> byId = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> byProviderSession = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> processedEvents = new(StringComparer.OrdinalIgnoreCase);

    public PaymentDto Add(PaymentDto payment)
    {
        byId[payment.PaymentId] = payment;
        if (!string.IsNullOrWhiteSpace(payment.ProviderSessionId))
        {
            byProviderSession[payment.ProviderSessionId] = payment.PaymentId;
        }
        return payment;
    }

    public PaymentDto? Get(string paymentId)
    {
        byId.TryGetValue(paymentId, out var payment);
        return payment;
    }

    public PaymentDto? GetByProviderSession(string providerSessionId)
    {
        if (!byProviderSession.TryGetValue(providerSessionId, out var paymentId))
        {
            return null;
        }

        return Get(paymentId);
    }

    public bool TryMarkEventProcessed(string eventId)
    {
        return processedEvents.TryAdd(eventId, 0);
    }

    public void Update(PaymentDto payment)
    {
        payment.UpdatedAtUtc = DateTime.UtcNow;
        byId[payment.PaymentId] = payment;
    }
}
