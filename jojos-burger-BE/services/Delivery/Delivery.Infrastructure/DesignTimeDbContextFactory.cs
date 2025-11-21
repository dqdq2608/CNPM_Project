using Delivery.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Delivery.Infrastructure;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DeliveryDbContext>
{
    public DeliveryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DeliveryDbContext>();

        // Local hard-coded connection string for migrations
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=deliverydb;Username=delivery;Password=Pass@word1"
        );

        return new DeliveryDbContext(optionsBuilder.Options);
    }
}
