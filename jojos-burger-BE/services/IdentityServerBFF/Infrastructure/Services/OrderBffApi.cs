using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using IdentityServerBFF.Application.Services;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace IdentityServerBFF.Infrastructure.Services;

public sealed class OrderBffApi : IOrderBffApi
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OrderBffApi> _logger;
    private readonly IGeocodingService _geocodingService;
    public OrderBffApi(
    IHttpClientFactory httpClientFactory,
    ILogger<OrderBffApi> logger,
    IGeocodingService geocoding)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _geocodingService = geocoding;
    }

    public async Task<String> CreateOrderFromBasketAsync(
    ClaimsPrincipal user,
    FrontCreateOrderRequest request,
    CancellationToken cancellationToken = default)
    {
        var userId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("User not authorized.");

        var userName = user.FindFirst("name")?.Value
               ?? user.FindFirst("email")?.Value
               ?? "Unknown";

        // 1️⃣ Lấy basket hiện tại từ Basket.API
        var basketClient = _httpClientFactory.CreateClient("basket");

        var basketReq = new HttpRequestMessage(HttpMethod.Get, "/api/basket");
        basketReq.Headers.Add("X-User-Sub", userId);

        var basketRes = await basketClient.SendAsync(basketReq, cancellationToken);
        if (!basketRes.IsSuccessStatusCode)
        {
            _logger.LogWarning("Get basket failed for user {UserId}: {StatusCode}", userId, basketRes.StatusCode);
            throw new InvalidOperationException($"Get basket failed: {basketRes.StatusCode}");
        }

        var basketJson = await basketRes.Content.ReadAsStringAsync(cancellationToken);

        var basket = JsonSerializer.Deserialize<CustomerBasketDto>(basketJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (basket == null || basket.Items.Count == 0)
        {
            throw new InvalidOperationException("Basket is empty.");
        }

        // 1️⃣ GEOCODING: lấy toạ độ khách
        var fullAddress = request.DeliveryAddress ?? "";
        var (customerLat, customerLon) = await _geocodingService.GeocodeAsync(fullAddress, cancellationToken);

        // 2️⃣ Lấy thông tin Restaurant từ Catalog API qua Kong
        var kongClient = _httpClientFactory.CreateClient("kong");

        // đúng path: /catalog/restaurants
        var rRes = await kongClient.GetAsync("api/catalog/restaurants", cancellationToken);
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

        // 2️⃣.3. Tính distance theo Haversine (km)
        var distanceKm = CalculateDistanceKm(restaurantLat, restaurantLon, customerLat, customerLon);

        // 2️⃣.4. Tính deliveryFee từ baseFee + perKm
        const decimal baseFee = 15;   // phí mở đầu
        const decimal perKm = 3;      // phí mỗi km thêm

        // làm tròn lên 1 chữ số thập phân hoặc nguyên tuỳ bạn
        var distanceRounded = (decimal)Math.Round(distanceKm, 1);

        // không cho nhỏ hơn 0
        if (distanceRounded < 0)
        {
            distanceRounded = 0;
        }

        // ví dụ: phí = baseFee + perKm * distanceKm
        var deliveryFee = baseFee + perKm * distanceRounded;

        // 3️⃣ TẠO ORDER TRONG ORDERING.API
        var orderingClient = _httpClientFactory.CreateClient("ordering");

        // ⚠️ Ở đây mình sử dụng CreateOrderRequestDto đã định nghĩa ở dưới class
        // để khớp với CreateOrderRequest mà Ordering.API đang mong đợi.
        // 2. Map basket sang Items cho Ordering.API (lấy dữ liệu chuẩn từ Basket)

        var orderPayload = new CreateOrderRequestDto
        {
            UserId = userId,
            UserName = userName,
            City = "Ho Chi Minh",
            Street = request.DeliveryAddress ?? string.Empty,
            State = "N/A",
            Country = "Vietnam",
            ZipCode = "700000",


            // Trong demo order eShop: dùng fake card
            CardNumber = "1234123412341234",
            CardHolderName = "Quan",
            CardExpiration = DateTime.UtcNow.AddYears(1),
            CardSecurityNumber = "123",
            CardTypeId = 1,


            Buyer = userName,

            DeliveryFee = deliveryFee,

            Items = basket.Items.Select(it => new BasketItemDto
            {
                Id = it.Id,
                ProductId = it.ProductId,
                ProductName = it.ProductName,      // ⭐ tên chuẩn từ Catalog
                UnitPrice = it.UnitPrice,          // ⭐ giá chuẩn từ Catalog
                OldUnitPrice = it.OldUnitPrice,
                Quantity = it.Quantity,
                PictureUrl = it.PictureUrl         // ⭐ ảnh chuẩn từ Catalog
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

        // 🔹 NEW: check body rỗng để tránh JsonException khó debug
        if (string.IsNullOrWhiteSpace(orderBody))
        {
            _logger.LogError(
                "Ordering API returned empty body when creating order. StatusCode: {StatusCode}",
                orderRes.StatusCode);

            throw new InvalidOperationException("Ordering API returned empty body when creating order.");
        }

        OrderCreatedResponse? created;
        try
        {
            created = JsonSerializer.Deserialize<OrderCreatedResponse>(
                orderBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "Failed to deserialize Ordering API response when creating order. Body: {Body}",
                orderBody);

            throw new InvalidOperationException("Unexpected response format from Ordering API when creating order.");
        }

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

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula
        const double R = 6371.0; // bán kính Trái Đất (km)

        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double angle)
    {
        return Math.PI * angle / 180.0;
    }

    public async Task<DeliveryQuoteResponse> GetDeliveryQuoteAsync(
    ClaimsPrincipal user,
    DeliveryQuoteRequest request,
    CancellationToken cancellationToken = default)
    {
        if (user?.Identity?.IsAuthenticated != true)
            throw new InvalidOperationException("User is not authenticated.");

        // 1️⃣ Geocode địa chỉ khách
        var (customerLat, customerLon) =
            await _geocodingService.GeocodeAsync(request.DeliveryAddress, cancellationToken);

        // 2️⃣ Lấy restaurant từ Catalog qua Kong (y như CreateOrderFromBasketAsync)
        var kongClient = _httpClientFactory.CreateClient("kong");
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

        var distanceKm = CalculateDistanceKm(
            restaurant.Latitude,
            restaurant.Longitude,
            customerLat,
            customerLon);

        const decimal baseFee = 15000m;
        const decimal perKm = 3000m;

        var distanceRounded = (decimal)Math.Round(distanceKm, 1);
        if (distanceRounded < 0) distanceRounded = 0;

        var deliveryFee = baseFee + perKm * distanceRounded;

        return new DeliveryQuoteResponse(distanceKm, deliveryFee);
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
        public decimal DeliveryFee { get; set; }

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

        [JsonPropertyName("lat")]
        public double Latitude { get; set; }

        [JsonPropertyName("lng")]
        public double Longitude { get; set; }
    }


}
