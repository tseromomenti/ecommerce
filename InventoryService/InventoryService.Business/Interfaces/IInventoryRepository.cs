using InventoryService.Business.Entities;

namespace InventoryService.Business.Interfaces
{
    public interface IInventoryRepository
    {
        Task<ProductDto?> GetProductByNameAsync(string productName);

        Task<ProductDto?> GetProductByIdAsync(int id);

        Task<IEnumerable<ProductDto>> GetAllProductsAsync();

        Task AddProductAsync(ProductDto product);

        Task UpdateProductAsync(ProductDto product);

        Task DeleteProductAsync(ProductDto product);

        Task<bool> DeleteProductByIdAsync(int productId);
    }
}
