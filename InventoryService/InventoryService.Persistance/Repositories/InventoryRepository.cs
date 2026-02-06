using AutoMapper;
using InventoryService.Business.Entities;
using InventoryService.Business.Interfaces;
using InventoryService.Persistance.Dtos;
using InventoryService.Persistance.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductEntity = InventoryService.Persistance.Dtos.ProductEntity;

namespace InventoryService.Persistance.Repositories
{
    public class InventoryRepository(
        InventoryDbContext context,
        IMapper mapper,
        ILogger<InventoryRepository> logger)
        : IInventoryRepository
    {
        public async Task<ProductDto?> GetProductByNameAsync(string productName)
        {
            var productDto = await context.Products
                .FirstOrDefaultAsync(p => p.ProductName == productName);
            
            return productDto != null ? mapper.Map<ProductDto>(productDto) : null;
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            var productDto = await context.Products.FindAsync(id);
            return productDto != null ? mapper.Map<ProductDto>(productDto) : null;
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            var productDtos = await context.Products.ToListAsync();
            return mapper.Map<IEnumerable<ProductDto>>(productDtos);
        }

        public async Task AddProductAsync(ProductDto product)
        {
            var productDto = mapper.Map<ProductEntity>(product);
            await context.Products.AddAsync(productDto);
            await context.SaveChangesAsync();
            product.Id = productDto.Id;
        }

        public async Task UpdateProductAsync(ProductDto product)
        {
            var existingProduct = await context.Products.FindAsync(product.Id);
            if (existingProduct == null)
            {
                return;
            }

            mapper.Map(product, existingProduct);
            await context.SaveChangesAsync();
        }

        public async Task DeleteProductAsync(ProductDto product)
        {
            var productDto = mapper.Map<ProductEntity>(product);
            context.Products.Remove(productDto);
            await context.SaveChangesAsync();
        }

        public async Task<bool> DeleteProductByIdAsync(int productId)
        {
            var existingProduct = await context.Products.FindAsync(productId);
            if (existingProduct == null)
            {
                return false;
            }

            context.Products.Remove(existingProduct);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
