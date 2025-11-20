using Delivery.Domain;
using Microsoft.EntityFrameworkCore;

namespace Delivery.Infrastructure;

public class DeliveryDbContext : DbContext
{
    public DeliveryDbContext(DbContextOptions<DeliveryDbContext> options)
        : base(options)
    {
    }

    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("delivery");

        modelBuilder.Entity<DeliveryOrder>(b =>
        {
            b.ToTable("deliveryorders");
            b.HasKey(x => x.Id);

            b.Property(x => x.Status)
             .HasConversion<int>();
        });
    }
}
