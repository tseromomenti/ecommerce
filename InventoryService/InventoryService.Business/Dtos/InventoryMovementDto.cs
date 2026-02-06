using Ecommerce.Library.Messaging;

namespace InventoryService.Business.Entities
{
    public class InventoryMovementDto
    {
        public string? Id { get; set; } // MongoDB ObjectId
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public EventType  MovementType { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new(); // Customer info, reason, etc.
    }
}
