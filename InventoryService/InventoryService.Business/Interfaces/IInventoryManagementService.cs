using Ecommerce.Library.Dtos;
using InventoryService.Business.Entities;

namespace InventoryService.Business.Interfaces;

public interface IInventoryManagementService
{
    Task<IEnumerable<ProductDto>> GetAllProductsAsync();

    Task<ProductDto?> GetProductByIdAsync(int productId);

    Task<bool> ProcessCreateOrderAsync(OrderDto order);

    Task<bool> ProcessUpdateOrderAsync(OrderDto order);

    Task<bool> ProcessCancelOrderAsync(OrderDto order);

    Task<OrderDto> GetProductStockAsync(string productName);

    Task AddProductToInventory(string productName, int quantity);

    Task<IEnumerable<InventoryMovementDto>> GetProductHistoryAsync(string productName);
}
