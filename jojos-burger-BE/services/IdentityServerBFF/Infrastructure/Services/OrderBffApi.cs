using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using IdentityServerBFF.Application.Services;

namespace IdentityServerBFF.Infrastructure.Services;

public sealed class OrderBffApi : IOrderBffApi
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OrderBffApi> _logger;
    private readonly IGeocodingService _geocoding;
    public OrderBffApi(
    IHttpClientFactory httpClientFactory,
    ILogger<OrderBffApi> logger,
    IGeocodingService geocoding)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _geocoding = geocoding;
    }

    public async Task<String> CreateOrderFromBasketAsync(
    ClaimsPrincipal user,
    FrontCreateOrderRequest request,
    CancellationToken cancellationToken = default)
    {
        var userId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("User not authorized.");

        // 1️⃣ GEOCODING: lấy toạ độ khách
        var fullAddress = request.DeliveryAddress ?? "";
        var (customerLat, customerLon) = await _geocoding.GeocodeAsync(fullAddress, cancellationToken);

        // 2️⃣ Lấy thông tin Restaurant từ Catalog API qua Kong
        var kongClient = _httpClientFactory.CreateClient("kong");

        // đúng path: /catalog/restaurants
        var rRes = await kongClient.GetAsync("/catalog/restaurants", cancellationToken);
        rRes.EnsureSuccessStatusCode();

        var rJson = await rRes.Content.ReadAsStringAsync(cancellationToken);
        var restaurants = JsonSerializer.Deserialize<List<RestaurantLocationDto>>(
            rJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        ) ?? new List<RestaurantLocationDto>();

        var restaurant = restaurants.FirstOrDefault(r => r.RestaurantId == request.RestaurantId);

        if (restaurant is null)
            throw new InvalidOperationException($"Restaurant {request.RestaurantId} was not found.");

        var restaurantLat = restaurant.Latitude;
        var restaurantLon = restaurant.Longitude;


        // 3️⃣ TẠO ORDER TRONG ORDERING.API
        var orderingClient = _httpClientFactory.CreateClient("ordering");

        // ⚠️ Ở đây mình sử dụng CreateOrderRequestDto đã định nghĩa ở dưới class
        // để khớp với CreateOrderRequest mà Ordering.API đang mong đợi.
        var orderPayload = new CreateOrderRequestDto
        {
            UserId = userId,
            UserName = user.Identity?.Name ?? userId,

            // Địa chỉ: dùng luôn DeliveryAddress người dùng nhập
            City = "Ho Chi Minh",
            Street = request.DeliveryAddress ?? "Unknown street",
            State = "N/A",
            Country = "Vietnam",
            ZipCode = "700000",

            // Payment info: fake dữ liệu demo cho đơn giản
            CardNumber = "4111111111111111",
            CardHolderName = user.Identity?.Name ?? "Demo User",
            CardExpiration = DateTime.UtcNow.AddYears(1),
            CardSecurityNumber = "123",
            CardTypeId = 1,

            Buyer = userId,

            // Items: tối thiểu phải có ProductId, Units; các field còn lại Ordering thường
            // chỉ dùng để mapping sang domain, nhưng để an toàn ta cứ set cơ bản.
            Items = request.Products.Select(p => new BasketItemDto
            {
                Id = p.Id.ToString(),
                ProductId = p.Id,
                ProductName = $"Product {p.Id}",
                UnitPrice = 0m,        // nếu Ordering tự lookup giá thì không cần,
                OldUnitPrice = 0m,     // còn nếu không thì đây là chỗ bạn có thể nối với Basket/Catalog
                Quantity = p.Quantity,
                PictureUrl = string.Empty
            }).ToList()
        };

        var orderReq = new HttpRequestMessage(HttpMethod.Post, "/api/orders?api-version=1.0")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(orderPayload),
                Encoding.UTF8,
                "application/json")
        };

        // 🔹 Thêm requestId vào header cho Ordering (idempotency)
        var requestId = Guid.NewGuid().ToString();
        orderReq.Headers.Add("x-requestid", requestId);
        orderReq.Headers.Add("requestId", requestId);

        var orderRes = await orderingClient.SendAsync(orderReq, cancellationToken);
        var orderBody = await orderRes.Content.ReadAsStringAsync(cancellationToken);

        if (!orderRes.IsSuccessStatusCode)
            throw new InvalidOperationException($"Create order failed: {orderRes.StatusCode} - {orderBody}");

        var created = JsonSerializer.Deserialize<OrderCreatedResponse>(
            orderBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (created is null || created.OrderId <= 0)
            throw new InvalidOperationException("Ordering API returned invalid order result.");

        // 4️⃣ GỌI DELIVERY SERVICE
        var deliveryClient = _httpClientFactory.CreateClient("delivery");

        var deliveryPayload = new
        {
            OrderId = created.OrderId,
            RestaurantLat = restaurantLat,
            RestaurantLon = restaurantLon,
            CustomerLat = customerLat,
            CustomerLon = customerLon
        };

        var deliveryReq = new HttpRequestMessage(HttpMethod.Post, "/api/deliveries")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(deliveryPayload),
                Encoding.UTF8,
                "application/json")
        };

        var deliveryRes = await deliveryClient.SendAsync(deliveryReq, cancellationToken);
        var deliveryBody = await deliveryRes.Content.ReadAsStringAsync(cancellationToken);

        if (!deliveryRes.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Delivery creation failed for Order {OrderId}. Status {Status}. Body: {Body}",
                created.OrderId,
                deliveryRes.StatusCode,
                deliveryBody
            );
        }

        var jsonResult = JsonSerializer.Serialize(created,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        return jsonResult;
    }


    public async Task<string> GetOrdersForUserAsync(
    ClaimsPrincipal user,
    CancellationToken cancellationToken = default)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException("User is not authenticated.");
        }

        var userId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new InvalidOperationException("Cannot determine user id (sub).");
        }

        var orderingClient = _httpClientFactory.CreateClient("ordering");

        // Gọi endpoint mới: /api/orders/byuser/{userId}?api-version=1.0
        var url = $"/api/orders/byuser/{userId}?api-version=1.0";

        var res = await orderingClient.GetAsync(url, cancellationToken);
        var body = await res.Content.ReadAsStringAsync(cancellationToken);

        if (!res.IsSuccessStatusCode)
        {
            _logger.LogWarning("Get orders for user {UserId} failed: {StatusCode} - {Body}",
                userId, res.StatusCode, body);

            throw new InvalidOperationException(
                $"Get orders failed: {(int)res.StatusCode} - {body}");
        }

        return body; // JSON mảng orders
    }

    public async Task<string> GetOrderDetailAsync(
    ClaimsPrincipal user,
    int orderId,
    CancellationToken cancellationToken = default)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException("User is not authenticated.");
        }

        var orderingClient = _httpClientFactory.CreateClient("ordering");

        // Ở eShop, orderNumber thường trùng với Id, nên dùng luôn
        var url = $"/api/orders/{orderId}?api-version=1.0";

        var res = await orderingClient.GetAsync(url, cancellationToken);
        var body = await res.Content.ReadAsStringAsync(cancellationToken);

        if (!res.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Get order detail failed. OrderId: {OrderId}, Status: {StatusCode} - {Body}",
                orderId, res.StatusCode, body);

            throw new InvalidOperationException(
                $"Get order detail failed: {(int)res.StatusCode} - {body}");
        }

        return body; // JSON chi tiết đơn hàng (có items)
    }

    public async Task<string> GetDeliveryForOrderAsync(
    ClaimsPrincipal user,
    int orderId,
    CancellationToken cancellationToken = default)
    {
        if (user?.Identity?.IsAuthenticated != true)
            throw new InvalidOperationException("User is not authenticated.");

        var deliveryClient = _httpClientFactory.CreateClient("delivery");

        var url = $"/api/deliveries/by-order/{orderId}";
        var res = await deliveryClient.GetAsync(url, cancellationToken);
        var body = await res.Content.ReadAsStringAsync(cancellationToken);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Delivery lookup failed: {(int)res.StatusCode} - {body}");

        return body;
    }



    // DTO nội bộ giống lúc trước
    private sealed class CustomerBasketDto
    {
        public string BuyerId { get; set; } = default!;
        public List<BasketItemDto> Items { get; set; } = new();
    }

    private sealed class BasketItemDto
    {
        public string Id { get; set; } = default!;   // ✅ giống Basket.API / Ordering

        public int ProductId { get; set; }
        public string ProductName { get; set; } = default!;
        public decimal UnitPrice { get; set; }
        public decimal OldUnitPrice { get; set; }
        public int Quantity { get; set; }
        public string PictureUrl { get; set; } = default!;
    }


    private sealed class CreateOrderRequestDto
    {
        public string UserId { get; set; } = default!;
        public string UserName { get; set; } = default!;

        public string City { get; set; } = default!;
        public string Street { get; set; } = default!;
        public string State { get; set; } = default!;
        public string Country { get; set; } = default!;
        public string ZipCode { get; set; } = default!;

        public string CardNumber { get; set; } = default!;
        public string CardHolderName { get; set; } = default!;
        public DateTime CardExpiration { get; set; }
        public string CardSecurityNumber { get; set; } = default!;
        public int CardTypeId { get; set; }

        public string Buyer { get; set; } = default!;

        // ✅ Gửi List<BasketItemDto> cho đúng với CreateOrderRequest.Items
        public List<BasketItemDto> Items { get; set; } = new();
    }

    // DTO trả về cho FE sau khi tạo Order + (tuỳ bước sau) tạo Delivery
    public sealed record OrderCreatedResponse
    {
        public int OrderId { get; init; }

        // Sau này nếu có tạo Delivery kèm theo thì gán vào, còn giờ có thể để null
        public int? DeliveryId { get; init; }
    }

    public sealed class RestaurantLocationDto
    {
        public Guid RestaurantId { get; set; }
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

}
