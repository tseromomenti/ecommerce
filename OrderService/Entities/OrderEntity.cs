namespace OrderService.Entities
{
    public class OrderEntity
    {
        public int Id { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }
    }
}
