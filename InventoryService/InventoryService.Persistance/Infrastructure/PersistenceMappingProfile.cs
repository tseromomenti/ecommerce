using AutoMapper;
using InventoryService.Business.Entities;
using InventoryService.Persistance.Documents;
using InventoryService.Persistance.Dtos;
using ProductEntity = InventoryService.Persistance.Dtos.ProductEntity;

namespace InventoryService.Persistance.Infrastructure
{
    public class PersistenceMappingProfile : Profile
    {
        public PersistenceMappingProfile()
        {
            // Entity to DTO mappings for SQL Server persistence
            CreateMap<ProductDto, ProductEntity>().ReverseMap()
                .AfterMap((src, dest) =>
                {
                    dest.ImageUrl = ProductImageResolver.GetImageUrl(src.ProductName);
                });

            // Entity to Document mappings for MongoDB persistence
            CreateMap<InventoryMovementDto, OrderDocumentEntity>()
                .ReverseMap();
        }
    }
}