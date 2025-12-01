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
            "Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.ydchytgmtmhrrjdceinh;Password=quandeptrai123;Ssl Mode=Require;Trust Server Certificate=true;Include Error Detail=true;"
        );

        return new DeliveryDbContext(optionsBuilder.Options);
    }
}
