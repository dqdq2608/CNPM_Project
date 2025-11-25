using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace IdentityServerBFF.Application.Services;

public class CheckoutOnlineBffApi : IPaymentApi
{
    private readonly HttpClient _kong;
    private readonly ILogger<CheckoutOnlineBffApi> _logger;

    // Dùng lại options kiểu Web để serialize/deserialize
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public CheckoutOnlineBffApi(
        HttpClient httpClient,
        ILogger<CheckoutOnlineBffApi> logger)
    {
        _kong = httpClient;   // HttpClient này đã trỏ vào Kong và có apikey
        _logger = logger;
    }

    public async Task<string> CheckoutOnlineAsync(
    int orderId,
    CancellationToken cancellationToken = default)
    {
        // chỉ lo phần payment, KHÔNG tạo Order nữa
        var paymentUrl = await TryGetPaymentUrlAsync(orderId, cancellationToken);

        var composed = new
        {
            orderId,
            paymentUrl
        };

        var json = JsonSerializer.Serialize(composed, JsonOpts);
        return json;
    }

    public async Task<string> CheckoutOnlineAsync(
        string bodyJson,
        CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid();

        // 1) Gọi Ordering để tạo Order
        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/orders?api-version=1.0")
        {
            Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
        };
        httpReq.Headers.Add("x-requestid", requestId.ToString());

        _logger.LogInformation(
            "[BFF] Calling Ordering via Kong: POST /api/orders, x-requestid={RequestId}",
            requestId);

        var resp = await _kong.SendAsync(httpReq, cancellationToken);
        var respJson = await resp.Content.ReadAsStringAsync(cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "[BFF] Ordering.CreateOrder failed. StatusCode={Status}, Body={Body}",
                resp.StatusCode,
                respJson);

            // Trả nguyên body lỗi từ Ordering cho FE
            return respJson;
        }

        // Parse CreateOrderResponse { OrderId, Total }
        CreateOrderResponseDto? order;
        try
        {
            order = JsonSerializer.Deserialize<CreateOrderResponseDto>(respJson, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BFF] Cannot parse CreateOrderResponse: {Body}", respJson);
            // fallback: trả nguyên JSON cũ
            return respJson;
        }

        if (order is null)
        {
            _logger.LogWarning("[BFF] CreateOrderResponse is null. Body={Body}", respJson);
            return respJson;
        }

        // 2) Gọi PaymentProcessor qua Kong để lấy paymentUrl từ cache
        //    => PaymentProcessor phải có endpoint: GET /api/payments/{orderId}
        var paymentUrl = await TryGetPaymentUrlAsync(order.OrderId, cancellationToken);

        // 3) Gói lại response cho FE
        var composed = new
        {
            orderId = order.OrderId,
            total = order.Total,
            paymentUrl = paymentUrl // có thể null nếu chưa có link kịp
        };

        var json = JsonSerializer.Serialize(composed, JsonOpts);
        return json;
    }

    private async Task<string?> TryGetPaymentUrlAsync(int orderId, CancellationToken ct)
    {
        try
        {
            // Gọi Kong: /api/payments/{orderId}
            _logger.LogInformation("[BFF] Fetching payment link via Kong for OrderId={OrderId}", orderId);

            var res = await _kong.GetAsync($"/api/payments/{orderId}", ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[BFF] GetPaymentLink failed. Status={Status}, Body={Body}",
                    res.StatusCode, body);
                return null;
            }

            // PaymentProcessor trả { orderId, paymentUrl }
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.TryGetProperty("paymentUrl", out var urlProp))
            {
                var url = urlProp.GetString();
                _logger.LogInformation(
                    "[BFF] Got payment link for OrderId={OrderId}: {Url}",
                    orderId, url);

                return url;
            }

            _logger.LogWarning(
                "[BFF] Response from PaymentProcessor does not contain `paymentUrl`. Body={Body}",
                body);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BFF] Exception while fetching payment link for OrderId={OrderId}", orderId);
            return null;
        }
    }

    private sealed record CreateOrderResponseDto(int OrderId, decimal Total);
}
