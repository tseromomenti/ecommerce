using Ecommerce.Library.Messaging;
using OrderService.Entities;

namespace OrderService
{
    public class OrderEvent
    {
        public EventType EventType { get; set; }
        public int OrderId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
