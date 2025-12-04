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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eShop.Catalog.API;

public sealed class RestaurantDto
{
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public double Lat { get; set; }   // NTS: Y = latitude
    public double Lng { get; set; }   // NTS: X = longitude
    public string AdminEmail { get; set; } = string.Empty;
    public RestaurantStatus Status { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
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

public sealed class UpdateRestaurantStatusRequest
{
    public RestaurantStatus Status { get; set; }
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
                .Where(r => !r.IsDeleted
                            && r.Status == RestaurantStatus.Active) // ⭐ Không trả về nhà hàng đã soft delete
                .OrderBy(x => x.Name)
                .Select(r => new RestaurantDto
                {
                    RestaurantId = r.RestaurantId,
                    Name = r.Name,
                    Address = r.Address,
                    Lat = r.Location != null ? r.Location.Y : 0,
                    Lng = r.Location != null ? r.Location.X : 0,
                    Status = r.Status,
                    IsDeleted = r.IsDeleted,
                    DeletedAt = r.DeletedAt,
                })
                .ToListAsync());

        // ⭐ Tạo nhà hàng + account admin bên IDS
        api.MapPost("/restaurants-with-admin", CreateRestaurantWithAdmin);
        api.MapGet("/admin/restaurants", GetRestaurantsForAdmin);
        // Cập nhật thông tin nhà hàng
        api.MapPut("/restaurants/{id:guid}", UpdateRestaurant);
        api.MapPut("/restaurants/{id:guid}/status", UpdateRestaurantStatus);

        // Xoá nhà hàng + tài khoản admin bên IDS
        api.MapDelete("/restaurants/{id:guid}", DeleteRestaurantWithAdmin);
        api.MapGet("/restaurants/{id:guid}/order-count", GetOrderCount);

        // Statistics
        api.MapGet("/admin/restaurant-orders/{restaurantId:guid}", GetRestaurantOrdersForAdmin);

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

        // IQueryable<CatalogItem> query = services.Context.CatalogItems.AsQueryable();

        var query =
            from c in services.Context.CatalogItems
            join r in services.Context.Restaurants
                on c.RestaurantId equals r.RestaurantId
            where !r.IsDeleted
                && r.Status == RestaurantStatus.Active   // ⭐ chỉ nhà hàng đang hoạt động
            select c;

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

        // IQueryable<CatalogItem> query = services.Context.CatalogItems
        //     .Where(c => c.Name.StartsWith(name));

        var query =
            from c in services.Context.CatalogItems
            join r in services.Context.Restaurants
                on c.RestaurantId equals r.RestaurantId
            where !r.IsDeleted
                && r.Status == RestaurantStatus.Active
                && c.Name.StartsWith(name)
            select c;

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

        // 2) Generate email trước khi call IDS (KHÔNG lưu trong DB)
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

        // 3) Tạo entity Restaurant trong Catalog (KHÔNG có AdminEmail)
        var restaurant = new Restaurant
        {
            RestaurantId = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Address = request.Address?.Trim() ?? string.Empty
        };

        if (request.Lat.HasValue && request.Lng.HasValue)
        {
            restaurant.Location = new Point(request.Lng.Value, request.Lat.Value)
            {
                SRID = 4326
            };
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

        // 5) Build response DTO (AdminEmail lấy từ biến local)
        var dto = new RestaurantDto
        {
            RestaurantId = restaurant.RestaurantId,
            Name = restaurant.Name,
            Address = restaurant.Address,
            Lat = restaurant.Location?.Y ?? 0,
            Lng = restaurant.Location?.X ?? 0,
            AdminEmail = adminEmail
        };

        return TypedResults.Ok((object)new
        {
            Restaurant = dto,
            Admin = adminResult,
            AdminEmail = adminEmail
        });
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

    public static async Task<Results<NoContent, NotFound, BadRequest<string>>>
        UpdateRestaurantStatus(
            Guid id,
            UpdateRestaurantStatusRequest request,
            CatalogContext context,
            IHttpClientFactory httpClientFactory)
    {
        if (request == null)
            return TypedResults.BadRequest("Request body is required.");

        if (!Enum.IsDefined(typeof(RestaurantStatus), request.Status))
            return TypedResults.BadRequest("Invalid restaurant status.");

        // ================================
        // 0) Lấy restaurant hiện tại
        // ================================
        var restaurant = await context.Restaurants
            .FirstOrDefaultAsync(r => r.RestaurantId == id);

        if (restaurant is null)
            return TypedResults.NotFound();

        var oldStatus = restaurant.Status;
        var newStatus = request.Status;

        // ================================
        // 1) LUÔN CHECK ORDERING TRƯỚC
        // ================================
        var orderingClient = httpClientFactory.CreateClient("ordering-api");

        try
        {
            var resp = await orderingClient.GetAsync(
                $"/api/internal/orders/by-restaurant/{id}");

            if (resp.IsSuccessStatusCode)
            {
                var orders = await resp.Content
                    .ReadFromJsonAsync<List<OrderSummaryLite>>()
                    ?? new List<OrderSummaryLite>();

                // đếm đơn còn đang xử lý
                int processing = orders.Count(o =>
                    !string.Equals(o.Status, "Completed", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
                );

                if (processing > 0)
                {
                    return TypedResults.BadRequest(
                        $"Nhà hàng vẫn còn {processing} đơn đang xử lý. Không thể đổi trạng thái."
                    );
                }
            }
            else if (resp.StatusCode != HttpStatusCode.NotFound)
            {
                var msg = await resp.Content.ReadAsStringAsync();
                return TypedResults.BadRequest(
                    $"Không kiểm tra được đơn hàng ở Ordering: {msg}"
                );
            }
            // 404 = không có đơn → OK
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(
                $"Lỗi khi gọi Ordering API: {ex.Message}"
            );
        }

        // ================================
        // 2) NẾU ĐỔI SANG CLOSED → CHECK DRONE
        // ================================
        if (newStatus == RestaurantStatus.Closed)
        {
            var deliveryClient = httpClientFactory.CreateClient("delivery-api");

            try
            {
                var droneResp = await deliveryClient.GetAsync(
                    $"/api/drones?restaurantId={id}");

                if (droneResp.IsSuccessStatusCode)
                {
                    var drones = await droneResp.Content
                        .ReadFromJsonAsync<List<DroneLite>>()
                        ?? new List<DroneLite>();

                    const int OFFLINE = 4; // DroneStatus.Offline

                    int notOffline = drones.Count(d => d.Status != OFFLINE);

                    if (notOffline > 0)
                    {
                        return TypedResults.BadRequest(
                            $"Không thể đóng cửa nhà hàng vì còn {notOffline} drone chưa ở trạng thái Offline."
                        );
                    }
                }
                else if (droneResp.StatusCode != HttpStatusCode.NotFound)
                {
                    var msg = await droneResp.Content.ReadAsStringAsync();
                    return TypedResults.BadRequest(
                        $"Không kiểm tra được drone ở Delivery: {msg}"
                    );
                }
                // 404 = không có drone → OK
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(
                    $"Lỗi khi gọi Delivery API: {ex.Message}"
                );
            }
        }

        // ================================
        // 3) GỌI IDS: activate/deactivate admin
        // ================================
        var idsClient = httpClientFactory.CreateClient("ids-admin-api");

        // Đóng cửa => deactivate
        if (newStatus == RestaurantStatus.Closed)
        {
            try
            {
                var resp = await idsClient.PostAsJsonAsync(
                    $"/api/restaurant-admins/by-restaurant/{id}/deactivate",
                    new { reason = "restaurant_closed" });

                if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.NotFound)
                {
                    var msg = await resp.Content.ReadAsStringAsync();
                    return TypedResults.BadRequest(
                        $"Không deactivate được tài khoản admin bên IDS: {msg}"
                    );
                }
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(
                    $"Lỗi khi gọi IDS (deactivate): {ex.Message}"
                );
            }
        }
        // Từ Closed → sang trạng thái khác (Active / Tạm ngưng) => activate lại
        else if (oldStatus == RestaurantStatus.Closed && newStatus != RestaurantStatus.Closed)
        {
            try
            {
                var resp = await idsClient.PostAsJsonAsync(
                    $"/api/restaurant-admins/by-restaurant/{id}/activate",
                    new { reason = "restaurant_reopened" });

                if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.NotFound)
                {
                    var msg = await resp.Content.ReadAsStringAsync();
                    return TypedResults.BadRequest(
                        $"Không activate được tài khoản admin bên IDS: {msg}"
                    );
                }
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest(
                    $"Lỗi khi gọi IDS (activate): {ex.Message}"
                );
            }
        }

        // ================================
        // 4) CẬP NHẬT TRẠNG THÁI TRONG CATALOG
        // ================================
        restaurant.Status = newStatus;
        await context.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    public static async Task<Ok<List<RestaurantDto>>> GetRestaurantsForAdmin(
        CatalogContext context,
        IHttpClientFactory httpClientFactory,
        bool? includeDeleted)
    {
        // Chuẩn bị query
        IQueryable<Restaurant> query = context.Restaurants.AsNoTracking();

        // Mặc định: chỉ lấy nhà hàng chưa bị xoá mềm
        // Chỉ khi includeDeleted == true mới lấy tất cả
        if (includeDeleted != true)
        {
            query = query.Where(r => !r.IsDeleted);
        }

        var restaurants = await query
            .OrderBy(x => x.Name)
            .ToListAsync();

        var result = new List<RestaurantDto>();

        foreach (var r in restaurants)
        {
            var dto = new RestaurantDto
            {
                RestaurantId = r.RestaurantId,
                Name         = r.Name,
                Address      = r.Address,
                Lat          = r.Location != null ? r.Location.Y : 0,
                Lng          = r.Location != null ? r.Location.X : 0,
                AdminEmail   = string.Empty,
                Status       = r.Status,
                IsDeleted    = r.IsDeleted,
                DeletedAt    = r.DeletedAt
            };

            var email = await FetchAdminEmailFromIdsAsync(r.RestaurantId, httpClientFactory);
            if (!string.IsNullOrWhiteSpace(email))
            {
                dto.AdminEmail = email;
            }

            result.Add(dto);
        }

        return TypedResults.Ok(result);
    }

    public static async Task<Results<NoContent, NotFound, BadRequest<object>>> DeleteRestaurantWithAdmin(
        Guid id,
        CatalogContext context,
        IHttpClientFactory httpClientFactory)
    {
        var restaurant = await context.Restaurants
            .FirstOrDefaultAsync(r => r.RestaurantId == id);

        if (restaurant is null)
            return TypedResults.NotFound();

        // 0) Chỉ cho xoá khi đã Closed
        if (restaurant.Status != RestaurantStatus.Closed)
        {
            return TypedResults.BadRequest((object)new
            {
                softDeleted = false,
                message = "Chỉ được xoá nhà hàng khi trạng thái là 'Đã đóng cửa'. Hãy đổi trạng thái sang Closed trước."
            });
        }

        // 1) Check ORDERING
        int orderCount = 0;
        var orderingClient = httpClientFactory.CreateClient("ordering-api");

        try
        {
            var orderResp = await orderingClient.GetAsync($"/api/internal/orders/by-restaurant/{id}");

            if (orderResp.IsSuccessStatusCode)
            {
                var orders = await orderResp.Content
                    .ReadFromJsonAsync<List<OrderSummaryLite>>()
                    ?? new List<OrderSummaryLite>();

                orderCount = orders.Count;
            }
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest((object)new
            {
                softDeleted = false,
                message = $"Không kiểm tra được đơn hàng từ Ordering: {ex.Message}"
            });
        }

        // 2) Check DELIVERY – số drone
        int droneCount = 0;
        var deliveryClient = httpClientFactory.CreateClient("delivery-api");

        try
        {
            var droneResp = await deliveryClient.GetAsync($"/api/drones?restaurantId={id}");

            if (droneResp.IsSuccessStatusCode)
            {
                var drones = await droneResp.Content
                    .ReadFromJsonAsync<List<DroneLite>>()
                    ?? new List<DroneLite>();

                droneCount = drones.Count;
            }
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest((object)new
            {
                softDeleted = false,
                message = $"Không kiểm tra được drone từ Delivery: {ex.Message}"
            });
        }

        // 3) Check CATALOG – số món ăn
        int itemCount = await context.CatalogItems
            .CountAsync(i => i.RestaurantId == id);

        bool hasRelations = orderCount > 0 || droneCount > 0 || itemCount > 0;

        // =====================================================
        // 4) CÓ RELATION → SOFT DELETE
        // =====================================================
        if (hasRelations)
        {
            // Gọi IDS deactivate cho chắc (nếu chưa deactivate lúc đổi trạng thái)
            var idsClientSoft = httpClientFactory.CreateClient("ids-admin-api");
            try
            {
                await idsClientSoft.PostAsJsonAsync(
                    $"/api/restaurant-admins/by-restaurant/{id}/deactivate",
                    new { reason = "soft_delete" });
            }
            catch
            {
                // best-effort, không chặn soft delete
            }

            restaurant.IsDeleted = true;
            restaurant.DeletedAt = restaurant.DeletedAt ?? DateTime.UtcNow;

            await context.SaveChangesAsync();

            return TypedResults.BadRequest((object)new
            {
                softDeleted = true,
                orderCount,
                droneCount,
                itemCount,
                message = "Nhà hàng đã được đánh dấu 'Đã đóng cửa' (soft delete) do vẫn còn dữ liệu liên quan (đơn hàng / drone / món)."
            });
        }

        // =====================================================
        // 5) KHÔNG CÒN ORDER / DRONE / ITEM → HARD DELETE
        // =====================================================

        // Xoá admin bên IDS
        var idsClient = httpClientFactory.CreateClient("ids-admin-api");
        try
        {
            await idsClient.DeleteAsync($"/api/restaurant-admins/by-restaurant/{id}");
        }
        catch
        {
            // có thể log, nhưng không fail xoá catalog
        }

        // Xoá luôn items (thực tế itemCount == 0 rồi, nhưng cho chắc)
        var items = await context.CatalogItems
            .Where(ci => ci.RestaurantId == id)
            .ToListAsync();

        if (items.Count > 0)
            context.CatalogItems.RemoveRange(items);

        context.Restaurants.Remove(restaurant);
        await context.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    static async Task<IResult> GetOrderCount(
        Guid id,
        IHttpClientFactory httpClientFactory)
    {
        var client = httpClientFactory.CreateClient("ordering-api");

        try
        {
            var resp = await client.GetAsync($"/api/internal/orders/by-restaurant/{id}");

            Console.WriteLine(
                $"[GetOrderCount] Call Ordering for Restaurant={id}, Status={resp.StatusCode}"
            );

            if (!resp.IsSuccessStatusCode)
                return Results.Ok(new { orderCount = 0 });

            var orders = await resp.Content.ReadFromJsonAsync<List<OrderSummaryLite>>()
                        ?? new List<OrderSummaryLite>();

            Console.WriteLine(
                $"[GetOrderCount] Restaurant={id} => {orders.Count} orders"
            );

            return Results.Ok(new { orderCount = orders.Count });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[GetOrderCount] ERROR for Restaurant={id}. {ex}"
            );
            return Results.Ok(new { orderCount = 0 });
        }
    }

    public static async Task<Results<Ok<List<OrderSummaryLite>>, BadRequest<string>>>
        GetRestaurantOrdersForAdmin(
            Guid restaurantId,
            IHttpClientFactory httpClientFactory)
    {
        var orderingClient = httpClientFactory.CreateClient("ordering-api");

        try
        {
            // Dùng internal API đã có sẵn trong Ordering
            var resp = await orderingClient.GetAsync(
                $"/api/internal/orders/by-restaurant/{restaurantId}");

            if (!resp.IsSuccessStatusCode)
            {
                var msg = await resp.Content.ReadAsStringAsync();
                return TypedResults.BadRequest(
                    $"Không lấy được đơn hàng từ Ordering: {msg}"
                );
            }

            var orders = await resp.Content
                .ReadFromJsonAsync<List<OrderSummaryLite>>()
                ?? new List<OrderSummaryLite>();

            return TypedResults.Ok(orders);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(
                $"Lỗi khi gọi Ordering API: {ex.Message}"
            );
        }
    }

    // DTO nhẹ để deserialize list order từ Ordering
    public sealed class OrderSummaryLite
    {
        [JsonPropertyName("orderNumber")]
        public int OrderNumber { get; set; }

        [JsonPropertyName("status")]   // 👈 ĐÚNG THEO JSON
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("deliveryFee")]
        public decimal DeliveryFee { get; set; }
    }
    // DTO nhẹ để đọc Drone từ Delivery API
    private sealed class DroneLite
    {
        public int Id { get; set; }

        // Delivery trả DroneStatus là enum int (0..4)
        public int Status { get; set; }
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

    // DTO dùng để đọc response từ IDS
    private sealed class RestaurantAdminLookupDto
    {
        public string Email { get; set; } = string.Empty;
    }

    // Helper gọi IDS để lấy email admin theo RestaurantId
    private static async Task<string> FetchAdminEmailFromIdsAsync(
        Guid restaurantId,
        IHttpClientFactory httpClientFactory)
    {
        var client = httpClientFactory.CreateClient("ids-admin-api");

        try
        {
            var resp = await client.GetAsync($"/api/restaurant-admins/by-restaurant/{restaurantId}");

            if (!resp.IsSuccessStatusCode)
                return string.Empty;

            var json = await resp.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json))
                return string.Empty;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Trường hợp phổ biến: trả về object { email: "...", ... }
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetEmailFromElement(root, out var email))
                    return email;
            }

            // Nếu trả về array, lấy phần tử đầu tiên
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                var first = root[0];
                if (first.ValueKind == JsonValueKind.Object &&
                    TryGetEmailFromElement(first, out var email))
                {
                    return email;
                }
            }

            return string.Empty;
        }
        catch
        {
            // IDS lỗi / JSON lỗi -> không làm vỡ API, chỉ không hiển thị email
            return string.Empty;
        }
    }

    // Tìm property email / Email / adminEmail ... trong 1 object JSON
    private static bool TryGetEmailFromElement(JsonElement element, out string email)
    {
        email = string.Empty;

        // duyệt tất cả property, tìm những key có chứa "email"
        foreach (var prop in element.EnumerateObject())
        {
            var name = prop.Name.ToLowerInvariant();
            if (name == "email" || name == "adminemail" || name.EndsWith("email"))
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    email = prop.Value.GetString() ?? string.Empty;
                    return !string.IsNullOrWhiteSpace(email);
                }
            }
        }

        return false;
    }

}
