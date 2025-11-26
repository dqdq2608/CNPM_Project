using System;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http.Json;                // ⭐ thêm
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;         // ⭐ thêm
using eShop.Catalog.API.Services;
using eShop.Catalog.API.Model;
using System.Net;

namespace eShop.Catalog.API;

public sealed class RestaurantDto
{
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public double Lat { get; set; }   // NTS: Y = latitude
    public double Lng { get; set; }   // NTS: X = longitude
    public string AdminEmail { get; set; } = string.Empty;
}

// ⭐ Request khi tạo nhà hàng + account admin
public sealed class CreateRestaurantWithAdminRequest
{
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public string? Email { get; set; } // email admin nhà hàng
}

public sealed record CatalogTypeDto(int Id, string Type, string PictureUri);

public static class CatalogApi
{
    public static IEndpointRouteBuilder MapCatalogApiV1(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/catalog");

        // -------- Items (query) --------
        api.MapGet("/items", GetAllItems);
        api.MapGet("/items/by", GetItemsByIds);
        api.MapGet("/items/{id:int}", GetItemById);
        api.MapGet("/items/by/{name:minlength(1)}", GetItemsByName);
        api.MapGet("/items/{catalogItemId:int}/pic", GetItemPictureById);

        // AI semantic search (optional)
        api.MapGet("/items/withsemanticrelevance/{text:minlength(1)}", GetItemsBySemanticRelevance);

        // Filter theo Type / Restaurant
        api.MapGet("/items/type/{typeId:int}/restaurant/{restaurantId:guid?}", GetItemsByTypeAndRestaurant);
        api.MapGet("/items/restaurant/{restaurantId:guid}", GetItemsByRestaurant);

        // -------- Lookups --------
        api.MapGet("/catalogtypes", async (CatalogContext context)
            => await context.CatalogTypes
                .OrderBy(x => x.Type)
                .ToListAsync());

        // Ảnh theo typeId
        api.MapGet("/catalogtypes/{id:int}/pic", async (int id, IWebHostEnvironment env) =>
        {
            var fileName = GetCatalogTypeImageFileName(id);
            if (string.IsNullOrWhiteSpace(fileName))
                return Results.NotFound();

            var physicalPath = Path.Combine(env.ContentRootPath, "Pics", fileName);
            if (!System.IO.File.Exists(physicalPath))
                return Results.NotFound();

            // Dùng Results.File với byte[] cho chắc, tránh vấn đề Results.PhysicalFile giữa các SDK
            var bytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
            var contentType = GetImageMimeTypeFromImageFileExtension(Path.GetExtension(fileName));
            return Results.File(bytes, contentType);
        });

        // Danh sách type kèm URL ảnh
        api.MapGet("/catalogtypes-with-pics", async (CatalogContext ctx) =>
        {
            // Base URL public qua Kong
            const string externalBase = "https://localhost:8443/api/catalog";

            var types = await ctx.CatalogTypes
                .AsNoTracking()
                .OrderBy(x => x.Type)
                .Select(t => new CatalogTypeDto(
                    t.Id,
                    t.Type,
                    $"{externalBase}/catalogtypes/{t.Id}/pic"
                ))
                .ToListAsync();

            return Results.Ok(types);
        });

        // Tạo mới CatalogType
        api.MapPost("/catalogtypes", CreateCatalogType);
        api.MapPut("/catalogtypes", UpdateCatalogTypeV1);
        api.MapDelete("/catalogtypes/{id:int}", DeleteCatalogType);

        // Trả DTO phẳng để tránh serialize NetTopologySuite Point
        api.MapGet("/restaurants", async (CatalogContext context) =>
            await context.Restaurants
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(r => new RestaurantDto
                {
                    RestaurantId = r.RestaurantId,
                    Name = r.Name,
                    Address = r.Address,
                    Lat = r.Location != null ? r.Location.Y : 0, // Y = Lat
                    Lng = r.Location != null ? r.Location.X : 0  // X = Lng
                })
                .ToListAsync());

        // ⭐ Tạo nhà hàng + account admin bên IDS
        api.MapPost("/restaurants-with-admin", CreateRestaurantWithAdmin);
        api.MapGet("/admin/restaurants", GetRestaurantsForAdmin);
        // Cập nhật thông tin nhà hàng
        api.MapPut("/restaurants/{id:guid}", UpdateRestaurant);

        // Xoá nhà hàng + tài khoản admin bên IDS
        api.MapDelete("/restaurants/{id:guid}", DeleteRestaurantWithAdmin);

        // -------- Items (mutations) --------
        api.MapPut("/items", UpdateItem);
        api.MapPost("/items", CreateItem);
        api.MapDelete("/items/{id:int}", DeleteItemById);

        return app;
    }

    // ---------- Queries ----------

    public static async Task<Results<Ok<PaginatedItems<CatalogItem>>, BadRequest<string>>> GetAllItems(
        [AsParameters] PaginationRequest paginationRequest,
        [AsParameters] CatalogServices services,
        int? typeId,
        Guid? restaurantId,
        bool? onlyAvailable)
    {
        var pageSize = paginationRequest.PageSize;
        var pageIndex = paginationRequest.PageIndex;

        IQueryable<CatalogItem> query = services.Context.CatalogItems.AsQueryable();

        if (typeId is not null)
            query = query.Where(c => c.CatalogTypeId == typeId);

        if (restaurantId is not null && restaurantId != Guid.Empty)
            query = query.Where(c => c.RestaurantId == restaurantId);

        if (onlyAvailable is true)
            query = query.Where(c => c.IsAvailable);

        var totalItems = await query.LongCountAsync();

        var itemsOnPage = await query
            .OrderBy(c => c.Name)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        return TypedResults.Ok(new PaginatedItems<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage));
    }

    public static async Task<Ok<List<CatalogItem>>> GetItemsByIds(
        [AsParameters] CatalogServices services,
        int[] ids)
    {
        var items = await services.Context.CatalogItems
            .Where(item => ids.Contains(item.Id))
            .ToListAsync();

        return TypedResults.Ok(items);
    }

    public static async Task<Results<Ok<object>, NotFound, BadRequest<string>>> GetItemById(
        [AsParameters] CatalogServices services,
        int id)
    {
        if (id <= 0)
        {
            return TypedResults.BadRequest("Id is not valid.");
        }

        var item = await services.Context.CatalogItems
            .Include(ci => ci.CatalogType)
            .Include(ci => ci.Restaurant)
            .SingleOrDefaultAsync(ci => ci.Id == id);

        if (item == null)
        {
            return TypedResults.NotFound();
        }

        // Project ra DTO ẩn danh rồi ÉP về object để phù hợp Ok<object>
        var dto = new
        {
            item.Id,
            item.Name,
            item.Description,
            item.Price,
            item.IsAvailable,
            item.EstimatedPrepTime,
            item.PictureFileName,
            item.CatalogTypeId,
            CatalogType = item.CatalogType?.Type,
            item.RestaurantId,
            Restaurant = item.Restaurant == null ? null : new
            {
                item.Restaurant.RestaurantId,
                item.Restaurant.Name,
                item.Restaurant.Address,
                Lat = item.Restaurant.Location?.Y ?? 0, // Y = latitude
                Lng = item.Restaurant.Location?.X ?? 0  // X = longitude
            }
        };

        return TypedResults.Ok((object)dto);
    }

    public static async Task<Ok<PaginatedItems<CatalogItem>>> GetItemsByName(
        [AsParameters] PaginationRequest paginationRequest,
        [AsParameters] CatalogServices services,
        string name,
        int? typeId,
        Guid? restaurantId)
    {
        var pageSize = paginationRequest.PageSize;
        var pageIndex = paginationRequest.PageIndex;

        IQueryable<CatalogItem> query = services.Context.CatalogItems
            .Where(c => c.Name.StartsWith(name));

        if (typeId is not null)
            query = query.Where(c => c.CatalogTypeId == typeId);

        if (restaurantId is not null && restaurantId != Guid.Empty)
            query = query.Where(c => c.RestaurantId == restaurantId);

        var totalItems = await query.LongCountAsync();

        var itemsOnPage = await query
            .OrderBy(c => c.Name)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        return TypedResults.Ok(new PaginatedItems<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage));
    }

    public static async Task<Results<NotFound, PhysicalFileHttpResult>> GetItemPictureById(
        CatalogContext context, IWebHostEnvironment environment, int catalogItemId)
    {
        var item = await context.CatalogItems.FindAsync(catalogItemId);

        if (item is null || string.IsNullOrWhiteSpace(item.PictureFileName))
            return TypedResults.NotFound();

        var path = GetFullPath(environment.ContentRootPath, item.PictureFileName);

        string imageFileExtension = Path.GetExtension(item.PictureFileName);
        string mimetype = GetImageMimeTypeFromImageFileExtension(imageFileExtension);
        DateTime lastModified = System.IO.File.GetLastWriteTimeUtc(path);

        return TypedResults.PhysicalFile(path, mimetype, lastModified: lastModified);
    }

    public static Results<NotFound, PhysicalFileHttpResult> GetCatalogTypePictureById(
     CatalogContext context,
     IWebHostEnvironment env,
     int typeId)
    {
        // Thư mục chứa ảnh
        var baseDir = Path.Combine(env.ContentRootPath, "Pics", "Types");

        // 1) Thử theo quy ước {id}.{ext}
        string? path = TryFindPictureById(baseDir, typeId);
        if (path is null)
        {
            // 2) Fallback: lấy tên type rồi thử theo tên (Burger/Drink/Combo/SideDish...)
            var typeName = context.CatalogTypes
                .AsNoTracking()
                .Where(t => t.Id == typeId)
                .Select(t => t.Type)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(typeName))
            {
                path = TryFindPictureByName(baseDir, typeName!);
            }
        }

        if (path is null || !System.IO.File.Exists(path))
            return TypedResults.NotFound();

        var ext = Path.GetExtension(path);
        var mime = GetImageMimeTypeFromImageFileExtension(ext);
        var lastModified = System.IO.File.GetLastWriteTimeUtc(path);
        return TypedResults.PhysicalFile(path, mime, lastModified: lastModified);

        // ------- helpers cục bộ -------
        static string? TryFindPictureById(string baseDir, int id)
        {
            foreach (var ext in new[] { ".webp", ".png", ".jpg", ".jpeg" })
            {
                var p = Path.Combine(baseDir, $"{id}{ext}");
                if (System.IO.File.Exists(p)) return p;
            }
            return null;
        }

        static string? TryFindPictureByName(string baseDir, string name)
        {
            // Chuẩn hoá tên: bỏ khoảng trắng, lower, thay space bằng '-', '_'…
            var candidates = new[]
            {
                name,
                name.Replace(" ", ""),
                name.Replace(" ", "-"),
                name.Replace(" ", "_")
            }
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

            foreach (var n in candidates)
            {
                foreach (var ext in new[] { ".webp", ".png", ".jpg", ".jpeg" })
                {
                    var p1 = Path.Combine(baseDir, $"{n}{ext}");          // Burger.webp
                    var p2 = Path.Combine(baseDir, $"{n.ToLower()}{ext}"); // burger.webp
                    if (System.IO.File.Exists(p1)) return p1;
                    if (System.IO.File.Exists(p2)) return p2;
                }
            }
            return null;
        }
    }

    // AI semantic (in-memory distance)
    public static async Task<Results<BadRequest<string>, Ok<PaginatedItems<CatalogItem>>>> GetItemsBySemanticRelevance(
        [AsParameters] PaginationRequest paginationRequest,
        [AsParameters] CatalogServices services,
        string text,
        int? typeId,
        Guid? restaurantId)
    {
        if (!services.CatalogAI.IsEnabled)
            return TypedResults.BadRequest("Semantic search is disabled.");

        var pageSize = paginationRequest.PageSize;
        var pageIndex = paginationRequest.PageIndex;

        var vector = await services.CatalogAI.GetEmbeddingAsync(text);
        if (vector is null)
            return TypedResults.BadRequest("Embedding generator is not available.");

        IQueryable<CatalogItem> baseQuery = services.Context.CatalogItems.AsNoTracking();

        if (typeId is not null)
            baseQuery = baseQuery.Where(c => c.CatalogTypeId == typeId);

        if (restaurantId is not null && restaurantId != Guid.Empty)
            baseQuery = baseQuery.Where(c => c.RestaurantId == restaurantId);

        var totalItems = await baseQuery.LongCountAsync();
        var all = await baseQuery.ToListAsync();

        var itemsWithDistance = all
            .Select(i => new { Item = i, Distance = CosineDistanceInMemory(i.Embedding, vector) })
            .OrderBy(x => x.Distance)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToList();

        if (services.Logger.IsEnabled(LogLevel.Debug))
        {
            services.Logger.LogDebug(
                "Semantic results from '{Text}': {Results}",
                text,
                string.Join(", ", itemsWithDistance.Select(i => $"{i.Item.Name} => {i.Distance:F4}"))
            );
        }

        var itemsOnPage = itemsWithDistance.Select(x => x.Item).ToList();
        return TypedResults.Ok(new PaginatedItems<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage));
    }

    public static async Task<Ok<PaginatedItems<CatalogItem>>> GetItemsByTypeAndRestaurant(
        [AsParameters] PaginationRequest paginationRequest,
        [AsParameters] CatalogServices services,
        int typeId,
        Guid? restaurantId)
    {
        var pageSize = paginationRequest.PageSize;
        var pageIndex = paginationRequest.PageIndex;

        IQueryable<CatalogItem> root = services.Context.CatalogItems.Where(c => c.CatalogTypeId == typeId);

        if (restaurantId is not null && restaurantId != Guid.Empty)
            root = root.Where(ci => ci.RestaurantId == restaurantId);

        var totalItems = await root.LongCountAsync();

        var itemsOnPage = await root
            .OrderBy(c => c.Name)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        return TypedResults.Ok(new PaginatedItems<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage));
    }

    public static async Task<Ok<PaginatedItems<CatalogItem>>> GetItemsByRestaurant(
        [AsParameters] PaginationRequest paginationRequest,
        [AsParameters] CatalogServices services,
        Guid restaurantId)
    {
        var pageSize = paginationRequest.PageSize;
        var pageIndex = paginationRequest.PageIndex;

        var root = services.Context.CatalogItems.Where(ci => ci.RestaurantId == restaurantId);

        var totalItems = await root.LongCountAsync();

        var itemsOnPage = await root
            .OrderBy(c => c.Name)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        return TypedResults.Ok(new PaginatedItems<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage));
    }

    // ---------- Mutations ----------

    public static async Task<Results<Created, NotFound<string>>> UpdateItem(
        [AsParameters] CatalogServices services,
        CatalogItem productToUpdate)
    {
        var catalogItem = await services.Context.CatalogItems.SingleOrDefaultAsync(i => i.Id == productToUpdate.Id);

        if (catalogItem == null)
            return TypedResults.NotFound($"Item with id {productToUpdate.Id} not found.");

        // Update fields (không cho đổi Id)
        var catalogEntry = services.Context.Entry(catalogItem);
        catalogEntry.CurrentValues.SetValues(productToUpdate);

        // Rebuild embedding nếu có AI
        catalogItem.Embedding = await services.CatalogAI.GetEmbeddingAsync(catalogItem);

        var priceEntry = catalogEntry.Property(i => i.Price);

        if (priceEntry.IsModified)
        {
            var priceChangedEvent = new ProductPriceChangedIntegrationEvent(
                catalogItem.Id, productToUpdate.Price, priceEntry.OriginalValue);

            await services.EventService.SaveEventAndCatalogContextChangesAsync(priceChangedEvent);
            await services.EventService.PublishThroughEventBusAsync(priceChangedEvent);
        }
        else
        {
            await services.Context.SaveChangesAsync();
        }

        return TypedResults.Created($"/api/catalog/items/{productToUpdate.Id}");
    }

    public static async Task<Created> CreateItem(
        [AsParameters] CatalogServices services,
        CatalogItem product)
    {
        // KHÔNG set Id để auto-increment
        var item = new CatalogItem
        {
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CatalogTypeId = product.CatalogTypeId,
            RestaurantId = product.RestaurantId,
            PictureFileName = product.PictureFileName,
            IsAvailable = product.IsAvailable,
            EstimatedPrepTime = product.EstimatedPrepTime,
            AvailableStock = product.AvailableStock,
            RestockThreshold = product.RestockThreshold,
            MaxStockThreshold = product.MaxStockThreshold,
            OnReorder = product.OnReorder
        };

        item.Embedding = await services.CatalogAI.GetEmbeddingAsync(item);

        services.Context.CatalogItems.Add(item);
        await services.Context.SaveChangesAsync();

        return TypedResults.Created($"/api/catalog/items/{item.Id}");
    }

    public static async Task<Results<NoContent, NotFound>> DeleteItemById(
        [AsParameters] CatalogServices services,
        int id)
    {
        var item = await services.Context.CatalogItems.SingleOrDefaultAsync(x => x.Id == id);

        if (item is null)
            return TypedResults.NotFound();

        services.Context.CatalogItems.Remove(item);
        await services.Context.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    // =======================  CATALOG TYPES CRUD  =======================

    public static async Task<Results<Created, BadRequest<string>>> CreateCatalogType(
        [AsParameters] CatalogServices services,
        CatalogType type)
    {
        if (string.IsNullOrWhiteSpace(type.Type))
            return TypedResults.BadRequest("Type name is required.");

        var entity = new CatalogType
        {
            Type = type.Type.Trim()
        };

        services.Context.CatalogTypes.Add(entity);
        await services.Context.SaveChangesAsync();

        return TypedResults.Created($"/api/catalog/catalogtypes/{entity.Id}");
    }

    public static async Task<Results<Created, BadRequest<string>, NotFound<string>>> UpdateCatalogTypeV1(
        [AsParameters] CatalogServices services,
        CatalogType type)
    {
        if (type.Id <= 0)
            return TypedResults.BadRequest("Id must be provided.");

        var existing = await services.Context.CatalogTypes
            .SingleOrDefaultAsync(t => t.Id == type.Id);

        if (existing is null)
            return TypedResults.NotFound($"CatalogType {type.Id} not found.");

        existing.Type = type.Type;
        await services.Context.SaveChangesAsync();

        return TypedResults.Created($"/api/catalog/catalogtypes/{type.Id}");
    }

    public static async Task<Results<NoContent, NotFound>> DeleteCatalogType(
        [AsParameters] CatalogServices services,
        int id)
    {
        var existing = await services.Context.CatalogTypes
            .SingleOrDefaultAsync(t => t.Id == id);

        if (existing is null)
            return TypedResults.NotFound();

        services.Context.CatalogTypes.Remove(existing);
        await services.Context.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    // ---------- RESTAURANT + ADMIN ACCOUNT ----------

    public static async Task<Results<Ok<object>, BadRequest<string>>> CreateRestaurantWithAdmin(
        CreateRestaurantWithAdminRequest? request,
        CatalogContext context,
        IHttpClientFactory httpClientFactory)
    {
        // 1) Validate request
        if (request is null)
            return TypedResults.BadRequest("Request body is required (JSON).");

        if (string.IsNullOrWhiteSpace(request.Name))
            return TypedResults.BadRequest("Restaurant name is required.");


        // 2) Generate email trước khi insert vào DB
        string adminEmail;

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            // đếm số restaurant hiện có
            var totalRestaurants = await context.Restaurants.CountAsync();
            var nextCode = (totalRestaurants + 1).ToString("000");

            adminEmail = $"owner.rest-{nextCode}@gmail.com";
        }
        else
        {
            adminEmail = request.Email.Trim();
        }


        // 3) Tạo entity Restaurant trước
        var restaurant = new Restaurant
        {
            RestaurantId = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Address = request.Address?.Trim() ?? string.Empty,
            AdminEmail = adminEmail   // ✔️ đặt đúng thời điểm (email đã tồn tại)
        };

        if (request.Lat.HasValue && request.Lng.HasValue)
        {
            restaurant.Location = new NetTopologySuite.Geometries.Point(
                request.Lng.Value,
                request.Lat.Value
            ) { SRID = 4326 };
        }

        context.Restaurants.Add(restaurant);
        await context.SaveChangesAsync();


        // 4) Call IDS để tạo admin account
        var client = httpClientFactory.CreateClient("ids-admin-api");

        var payload = new
        {
            RestaurantId = restaurant.RestaurantId.ToString(),
            RestaurantName = restaurant.Name,
            Email = adminEmail
        };

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("/api/restaurant-admins", payload);
        }
        catch (Exception ex)
        {
            // rollback nếu IDS lỗi
            context.Restaurants.Remove(restaurant);
            await context.SaveChangesAsync();

            return TypedResults.BadRequest($"Lỗi khi gọi IDS: {ex.Message}");
        }

        if (!response.IsSuccessStatusCode)
        {
            // rollback nếu tạo user thất bại
            context.Restaurants.Remove(restaurant);
            await context.SaveChangesAsync();

            var err = await response.Content.ReadAsStringAsync();
            return TypedResults.BadRequest($"Không tạo được tài khoản admin nhà hàng: {err}");
        }

        var adminResult = await response.Content.ReadFromJsonAsync<object>();


        // 5) Build response
        var dto = new RestaurantDto
        {
            RestaurantId = restaurant.RestaurantId,
            Name = restaurant.Name,
            Address = restaurant.Address,
            Lat = restaurant.Location?.Y ?? 0,
            Lng = restaurant.Location?.X ?? 0,
            AdminEmail = restaurant.AdminEmail
        };

        return TypedResults.Ok((object)new
        {
            Restaurant = dto,
            Admin = adminResult,
            AdminEmail = adminEmail
        });
    }

    public static async Task<Ok<List<RestaurantDto>>> GetRestaurantsForAdmin(
        CatalogContext context)
    {
        var list = await context.Restaurants
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(r => new RestaurantDto
            {
                RestaurantId = r.RestaurantId,
                Name = r.Name,
                Address = r.Address,
                Lat = r.Location != null ? r.Location.Y : 0,
                Lng = r.Location != null ? r.Location.X : 0,
                AdminEmail = r.AdminEmail ?? string.Empty
            })
            .ToListAsync();

        return TypedResults.Ok(list);
    }
    public static async Task<Results<NoContent, NotFound, BadRequest<string>>> UpdateRestaurant(
        Guid id,
        CreateRestaurantWithAdminRequest request,
        CatalogContext context)
    {
        if (request is null)
            return TypedResults.BadRequest("Request body is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return TypedResults.BadRequest("Restaurant name is required.");

        var restaurant = await context.Restaurants
            .FirstOrDefaultAsync(r => r.RestaurantId == id);

        if (restaurant is null)
            return TypedResults.NotFound();

        restaurant.Name    = request.Name.Trim();
        restaurant.Address = request.Address?.Trim() ?? string.Empty;

        if (request.Lat.HasValue && request.Lng.HasValue)
        {
            restaurant.Location = new Point(request.Lng.Value, request.Lat.Value)
            {
                SRID = 4326
            };
        }
        else
        {
            restaurant.Location = null;
        }

        // (tuỳ nhu cầu) KHÔNG đổi AdminEmail ở đây cho dễ quản lý
        await context.SaveChangesAsync();

        return TypedResults.NoContent();
    }
    public static async Task<Results<NoContent, NotFound, BadRequest<string>>> DeleteRestaurantWithAdmin(
        Guid id,
        CatalogContext context,
        IHttpClientFactory httpClientFactory)
    {
        var restaurant = await context.Restaurants
            .FirstOrDefaultAsync(r => r.RestaurantId == id);

        if (restaurant is null)
            return TypedResults.NotFound();

        // -----------------------------
        // 1) Xoá tài khoản admin bên IDS
        // -----------------------------
        if (!string.IsNullOrWhiteSpace(restaurant.AdminEmail))
        {
            var client = httpClientFactory.CreateClient("ids-admin-api");
            var encodedEmail = Uri.EscapeDataString(restaurant.AdminEmail);

            HttpResponseMessage resp;
            try
            {
                resp = await client.DeleteAsync($"/api/restaurant-admins/by-email/{encodedEmail}");
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest($"Lỗi khi gọi IDS: {ex.Message}");
            }

            // Nếu IDS báo NotFound thì coi như người dùng đã bị xoá → bỏ qua
            if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.NotFound)
            {
                var msg = await resp.Content.ReadAsStringAsync();
                return TypedResults.BadRequest($"Không xoá được tài khoản admin bên IDS: {msg}");
            }
        }

        // -----------------------------
        // 2) Xoá toàn bộ món thuộc nhà hàng
        // -----------------------------
        var items = await context.CatalogItems
            .Where(ci => ci.RestaurantId == id)
            .ToListAsync();

        if (items.Count > 0)
            context.CatalogItems.RemoveRange(items);

        // -----------------------------
        // 3) Xoá nhà hàng trong Catalog
        // -----------------------------
        context.Restaurants.Remove(restaurant);
        await context.SaveChangesAsync();

        return TypedResults.NoContent();
    }


    // ---------- Helpers ----------

    static string GetCatalogTypeImageFileName(int typeId) => typeId switch
    {
        1 => "burger.jpg",      // Burger
        2 => "drink.jpg",       // Drink
        3 => "combo.avif",      // Combo
        4 => "side_dishes.jpg", // Side Dish
        _ => null!
    };

    static string GetImageMimeTypeFromImageFileExtension(string extension) => extension.ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".bmp" => "image/bmp",
        ".tiff" => "image/tiff",
        ".wmf" => "image/wmf",
        ".jp2" => "image/jp2",
        ".svg" => "image/svg+xml",
        ".webp" => "image/webp",
        ".avif" => "image/avif",
        _ => "application/octet-stream",
    };

    public static string GetFullPath(string contentRootPath, string pictureFileName) =>
        System.IO.Path.Combine(contentRootPath, "Pics", pictureFileName);

    // Cosine distance in-memory (tránh conflict với pgvector EF)
    private static double CosineDistanceInMemory(Pgvector.Vector? a, Pgvector.Vector? b)
    {
        if (a is null || b is null) return 1.0;

        var va = a.ToArray();
        var vb = b.ToArray();
        int len = Math.Min(va.Length, vb.Length);

        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < len; i++)
        {
            double x = va[i];
            double y = vb[i];
            dot += x * y;
            na += x * x;
            nb += y * y;
        }

        if (na == 0 || nb == 0) return 1.0;
        double sim = dot / (Math.Sqrt(na) * Math.Sqrt(nb));
        return 1 - sim;
    }
}
