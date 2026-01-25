using AutoMapper;
using InventoryService.Business.Interfaces;
using InventoryService.Business.Entities;
using Ecommerce.Library.Dtos;
using Ecommerce.Library.Messaging;

namespace InventoryService.Business.Services
{
    public class InventoryManagementService(
        IInventoryRepository inventoryRepository,
        IDocumentRepository documentRepository,
        ILogger<InventoryManagementService> logger) : IInventoryManagementService
    {
        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            var products = (await inventoryRepository.GetAllProductsAsync()).ToArray();

            return products;
        }

        public async Task<ProductDto> GetProductByIdAsync(int productId)
        {
            var product = await inventoryRepository.GetProductByIdAsync(productId);

            return product;
        }

        public async Task<bool> ProcessCreateOrderAsync(OrderDto order)
        {
            // check if product exists
            var product = await inventoryRepository.GetProductByNameAsync(order.ProductName);

            if (product == null)
            {
                logger.LogWarning("Product {ProductName} not found", order.ProductName);
                return false;
            }

            // check if stock is sufficient
            if (product.AvailableStock < order.Quantity)
            {
                logger.LogWarning("Insufficient stock for product {ProductName}. Available: {AvailableStock}, Requested: {RequestedQuantity}",
                    product.ProductName, product.AvailableStock, order.Quantity);
                return false;
            }

            // reserve stock
            product.AvailableStock -= order.Quantity;
            product.ReservedStock += order.Quantity;

            await inventoryRepository.UpdateProductAsync(product);

            // record movement
            var movement = new InventoryMovementDto
            {
                OrderId = order.Id,
                ProductId = product.Id,
                ProductName = product.ProductName,
                Quantity = order.Quantity,
                MovementType = EventType.Created,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            await documentRepository.AddDocumentAsync(movement);

            return true;
        }

        public async Task<bool> ProcessUpdateOrderAsync(OrderDto order)
        {
            // check if product exists
            var product = await inventoryRepository.GetProductByNameAsync(order.ProductName);
            if (product == null)
            {
                logger.LogWarning("Product {ProductName} not found", order.ProductName);
                return false;
            }

            // check old order
            var lastOrderMovement = await documentRepository.GetDocumentByOrderAsync(order.Id);
            if (lastOrderMovement == null)
            {
                logger.LogWarning("Previous order {OrderId} not found", order.Id);
                return false;
            }

            // check if stock is sufficient
            if (product.AvailableStock + lastOrderMovement.Quantity < order.Quantity)
            {
                logger.LogWarning("Insufficient stock for product {ProductName}. Available: {AvailableStock}, Requested: {RequestedQuantity}",
                    product.ProductName, product.AvailableStock, order.Quantity);
                return false;
            }

            // reset available stock
            product.AvailableStock += lastOrderMovement.Quantity;
            product.ReservedStock -= lastOrderMovement.Quantity;

            // update reservation
            product.AvailableStock -= order.Quantity;
            product.ReservedStock += order.Quantity;

            await inventoryRepository.UpdateProductAsync(product);

            // record movement
            var movement = new InventoryMovementDto
            {
                OrderId = order.Id,
                ProductId = product.Id,
                ProductName = product.ProductName,
                Quantity = order.Quantity,
                MovementType = EventType.Updated,
                UpdatedAt = DateTime.UtcNow,
            };

            await documentRepository.AddDocumentAsync(movement);

            return true;
        }

        public async Task<bool> ProcessCancelOrderAsync(OrderDto order)
        {
            // check if product exists
            var product = await inventoryRepository.GetProductByNameAsync(order.ProductName);

            if (product == null)
            {
                logger.LogWarning("Product {ProductName} not found", order.ProductName);
                return false;
            }

            // remove reservation
            product.AvailableStock += order.Quantity;
            product.ReservedStock -= order.Quantity;

            await inventoryRepository.UpdateProductAsync(product);

            // record movement
            var movement = new InventoryMovementDto
            {
                OrderId = order.Id,
                ProductId = product.Id,
                ProductName = product.ProductName,
                Quantity = order.Quantity,
                MovementType = EventType.Deleted,
                UpdatedAt = DateTime.UtcNow,
            };

            await documentRepository.AddDocumentAsync(movement);

            return true;
        }

        public async Task AddProductToInventory(string productName, int quantity)
        {
            var product = await inventoryRepository.GetProductByNameAsync(productName);

            if (product == null)
            {
                product = new ProductDto
                {
                    ProductName = productName,
                    AvailableStock = quantity,
                    ReservedStock = 0
                };
                await inventoryRepository.AddProductAsync(product);
            }
            else
            {
                product.AvailableStock += quantity;
                await inventoryRepository.UpdateProductAsync(product);
            }
        }

        public Task<OrderDto> GetProductStockAsync(string productName)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<InventoryMovementDto>> GetProductHistoryAsync(string productName)
        {
            return await documentRepository.GetDocumentsByProductAsync(productName)
                .ContinueWith(task => task.Result);
        }
    }
}
