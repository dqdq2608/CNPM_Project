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

    public OrderBffApi(IHttpClientFactory httpClientFactory, ILogger<OrderBffApi> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> CreateOrderFromBasketAsync(
        ClaimsPrincipal user,
        FrontCreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException("User is not authenticated.");
        }

        var userId = user.FindFirst("sub")?.Value;
        var userName = user.FindFirst("name")?.Value
                       ?? user.FindFirst("email")?.Value
                       ?? "Unknown";

        if (string.IsNullOrEmpty(userId))
        {
            throw new InvalidOperationException("User id (sub) is missing.");
        }

        if (request?.products == null || request.products.Count == 0)
        {
            throw new InvalidOperationException("Products is empty.");
        }

        // 1. Lấy basket hiện tại từ Basket.API
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

        // 2. Map sang CreateOrderRequestDto cho Ordering.API
        var orderPayload = new CreateOrderRequestDto
        {
            UserId = userId,
            UserName = userName,

            City = "HCM",
            Street = "Some street",
            State = "HCMC",
            Country = "VN",
            ZipCode = "700000",

            CardNumber = "1234123412341234",
            CardHolderName = userName,
            CardExpiration = DateTime.UtcNow.AddYears(1),
            CardSecurityNumber = "123",
            CardTypeId = 1,

            Buyer = userName,
            Items = basket.Items.Select(it => new BasketItemDto
            {
                Id = it.Id,
                ProductId = it.ProductId,
                ProductName = it.ProductName,
                UnitPrice = it.UnitPrice,
                OldUnitPrice = it.OldUnitPrice,
                Quantity = it.Quantity,        // ✅ đúng tên property
                PictureUrl = it.PictureUrl
            }).ToList()

        };

        // 3. Gửi sang Ordering.API
        var orderingClient = _httpClientFactory.CreateClient("ordering");

        var content = new StringContent(
            JsonSerializer.Serialize(orderPayload),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var orderingReq = new HttpRequestMessage(HttpMethod.Post, "/api/orders?api-version=1.0")
        {
            Content = content
        };
        orderingReq.Headers.Add("x-requestid", Guid.NewGuid().ToString());

        var orderingRes = await orderingClient.SendAsync(orderingReq, cancellationToken);
        var orderingBody = await orderingRes.Content.ReadAsStringAsync(cancellationToken);

        if (!orderingRes.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Create order failed for user {UserId}: {StatusCode} - {Body}",
                userId, orderingRes.StatusCode, orderingBody);

            throw new InvalidOperationException(
                $"Create order failed: {(int)orderingRes.StatusCode} - {orderingBody}");
        }

        // Trả raw JSON lại cho BFF endpoint, FE có thể xài nếu cần
        return orderingBody;
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

}
