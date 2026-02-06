namespace OrderService.Entities
{
    public class OrderRequest
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
