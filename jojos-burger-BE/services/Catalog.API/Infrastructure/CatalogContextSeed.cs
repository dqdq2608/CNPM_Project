using System.Text.Json;
using eShop.Catalog.API.Model;
using eShop.Catalog.API.Services;
using Pgvector;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using NetTopologySuite.Geometries;

namespace eShop.Catalog.API.Infrastructure;

public partial class CatalogContextSeed(
    IWebHostEnvironment env,
    IOptions<CatalogOptions> settings,
    ICatalogAI catalogAI,
    ILogger<CatalogContextSeed> logger) : IDbSeeder<CatalogContext>
{
    public async Task SeedAsync(CatalogContext context)
    {
        var contentRootPath = env.ContentRootPath;
        var sourcePath = Path.Combine(contentRootPath, "Setup", "catalog.json");

        // Đảm bảo vector type load vào Npgsql
        context.Database.OpenConnection();
        ((NpgsqlConnection)context.Database.GetDbConnection()).ReloadTypes();

        // 1️⃣ Seed CatalogTypes
        if (!context.CatalogTypes.Any())
        {
            var types = new List<CatalogType>
            {
                new("Burger"),
                new("Drink"),
                new("Combo"),
                new("Side Dish")
            };

            await context.CatalogTypes.AddRangeAsync(types);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded CatalogTypes: {Count}", types.Count);
        }

        // 2️⃣ Seed Restaurants
        if (!context.Restaurants.Any())
        {
            var restaurants = new List<Restaurant>
            {
                new()
                {
                    RestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Name = "Jojo Burger - Q1",
                    Address = "123 Lê Lợi, Q1, TP.HCM",
                    Location = new Point(106.7000, 10.7730) { SRID = 4326 }
                },
                new()
                {
                    RestaurantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    Name = "Jojo Burger - Q3",
                    Address = "45 Võ Văn Tần, Q3, TP.HCM",
                    Location = new Point(106.6870, 10.7790) { SRID = 4326 }
                }
            };

            await context.Restaurants.AddRangeAsync(restaurants);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded Restaurants: {Count}", restaurants.Count);
        }

        // 3️⃣ Seed CatalogItems từ file catalog.json
        if (!context.CatalogItems.Any())
        {
            if (!File.Exists(sourcePath))
            {
                logger.LogWarning("No catalog.json found at {Path}. Skipping item seed.", sourcePath);
                return;
            }

            var json = await File.ReadAllTextAsync(sourcePath);
            var sourceItems = JsonSerializer.Deserialize<List<CatalogSourceEntry>>(json);

            if (sourceItems is null || sourceItems.Count == 0)
            {
                logger.LogWarning("catalog.json is empty or invalid. Skipping item seed.");
                return;
            }

            var typeIdsByName = await context.CatalogTypes.ToDictionaryAsync(x => x.Type, x => x.Id);
            var restaurantIds = await context.Restaurants.Select(r => r.RestaurantId).ToListAsync();

            var catalogItems = new List<CatalogItem>();
            int restIndex = 0;

            foreach (var src in sourceItems)
            {
                // Nếu type trong JSON không hợp lệ thì mặc định là "Burger"
                var typeId = typeIdsByName.ContainsKey(src.Type)
                    ? typeIdsByName[src.Type]
                    : typeIdsByName["Burger"];

                var restaurantId = restaurantIds[restIndex % restaurantIds.Count];
                restIndex++;

                catalogItems.Add(new CatalogItem
                {
                    Name = src.Name,
                    Description = src.Description,
                    Price = src.Price,
                    CatalogTypeId = typeId,
                    RestaurantId = restaurantId,
                    PictureFileName = $"{Guid.NewGuid()}.webp", // tạo random filename để tránh trùng
                    IsAvailable = true,
                    AvailableStock = 100,
                    MaxStockThreshold = 200,
                    RestockThreshold = 10,
                    EstimatedPrepTime = 10
                });
            }

            if (catalogAI.IsEnabled)
            {
                logger.LogInformation("Generating {NumItems} embeddings...", catalogItems.Count);
                IReadOnlyList<Vector> embeddings = await catalogAI.GetEmbeddingsAsync(catalogItems);
                for (int i = 0; i < catalogItems.Count; i++)
                {
                    catalogItems[i].Embedding = embeddings[i];
                }
            }

            await context.CatalogItems.AddRangeAsync(catalogItems);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded CatalogItems: {Count}", catalogItems.Count);
        }
    }

    private class CatalogSourceEntry
    {
        public string Type { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public decimal Price { get; set; }
    }
}
