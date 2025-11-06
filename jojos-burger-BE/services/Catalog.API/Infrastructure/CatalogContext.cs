using eShop.Catalog.API.Model;
using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.API.Infrastructure;

/// <remarks>
/// Add migrations inside the 'Catalog.API' project folder:
///   dotnet ef migrations add --context CatalogContext <migration-name>
/// </remarks>
public class CatalogContext : DbContext
{
    private readonly IConfiguration _configuration;

    public CatalogContext(DbContextOptions<CatalogContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    // DbSets
    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
    public DbSet<CatalogType> CatalogTypes => Set<CatalogType>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // PostgreSQL extensions
        builder.HasPostgresExtension("vector");   // pgvector for embeddings
        builder.HasPostgresExtension("postgis");  // PostGIS for Restaurant.Location

        // Apply entity configurations
        builder.ApplyConfiguration(new Infrastructure.EntityConfigurations.CatalogItemEntityTypeConfiguration());
        builder.ApplyConfiguration(new Infrastructure.EntityConfigurations.CatalogTypeEntityTypeConfiguration());
        builder.ApplyConfiguration(new Infrastructure.EntityConfigurations.RestaurantEntityTypeConfiguration());

        // Outbox table (IntegrationEventLogEF)
        builder.UseIntegrationEventLogs();
    }
}
