using InventoryService.Persistance.Dtos;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Persistance.Infrastructure
{
public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<ProductEntity> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProductEntity>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);
    }
}
}
