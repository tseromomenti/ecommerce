namespace InventoryService.Business.Entities
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Subcategory { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string AttributesJson { get; set; } = "{}";
        public string CurrencyCode { get; set; } = "USD";
        public bool IsActive { get; set; } = true;
        public string ImageUrl { get; set; } = string.Empty;
        public int AvailableStock { get; set; }
        public int ReservedStock { get; set; }
        public decimal Price { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
