using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Payment.Providers.Abstractions;
using Payment.Providers.PayOS;
using Microsoft.Extensions.Logging;

namespace Payment.Providers.PayOS;
// 🔹 data bên trong field "data"
internal sealed class PayOsCreatePaymentData
{
    public string? checkoutUrl { get; set; }
    public long orderCode { get; set; }
    public int amount { get; set; }
    public string? status { get; set; }
}

// 🔹 response wrapper { code, desc, data, signature }
internal sealed class PayOsCreatePaymentResponse
{
    public string? code { get; set; }
    public string? desc { get; set; }
    public PayOsCreatePaymentData? data { get; set; }
    public string? signature { get; set; }
}

public class PayOsPaymentProvider : IPaymentProvider
{
    private readonly HttpClient _httpClient;
    private readonly PayOsOptions _options;
    private readonly ILogger<PayOsPaymentProvider> _logger;

    public PayOsPaymentProvider(HttpClient httpClient, IOptions<PayOsOptions> options, ILogger<PayOsPaymentProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PaymentResult> CreatePaymentAsync(OrderPaymentData order)
    {
        if (!int.TryParse(order.OrderId, out var orderIdInt))
        {
            _logger.LogError("Invalid OrderId '{OrderId}' when creating PayOS payment", order.OrderId);
            return PaymentResult.Failed("INVALID_ORDER_ID", "OrderId is not a valid integer");
        }
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 100000;
        long orderCode = orderIdInt * 100000 + ts;

        _logger.LogInformation(
            "[PAYOS] Encoded orderCode = {orderCode} from OrderId = {orderId}",
            orderCode, orderIdInt
        );
        var amountInt = (int)order.Amount;
        var returnUrl = order.ReturnUrl ?? "https://localhost:3000/payment/success";
        var cancelUrl = order.CancelUrl ?? "https://localhost:3000/payment/cancel";
        var desc = order.Description ?? $"Thanh toán đơn hàng {order.OrderId}";

        // rawSignature theo docs PayOS
        var rawSignature =
            $"amount={amountInt}" +
            $"&cancelUrl={cancelUrl}" +
            $"&description={desc}" +
            $"&orderCode={orderCode}" +
            $"&returnUrl={returnUrl}";

        var signature = SignHmacSha256(rawSignature, _options.ChecksumKey);

        var payload = new
        {
            orderCode = orderCode,
            amount = amountInt,
            description = desc,
            cancelUrl = cancelUrl,
            returnUrl = returnUrl,
            signature = signature
        };

        var url = _options.BaseUrl.TrimEnd('/') + "/v2/payment-requests";

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };

        request.Headers.Add("x-client-id", _options.ClientId);
        request.Headers.Add("x-api-key", _options.ApiKey);
        if (!string.IsNullOrWhiteSpace(_options.PartnerCode))
        {
            request.Headers.Add("x-partner-code", _options.PartnerCode);
        }

        try
        {
            using var response = await _httpClient.SendAsync(request);
            var bodyText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return PaymentResult.Failed(
                    ((int)response.StatusCode).ToString(),
                    $"payOS HTTP {response.StatusCode}: {bodyText}"
                );
            }

            // Parse đúng cấu trúc wrapper { code, desc, data{ checkoutUrl } }
            var data = System.Text.Json.JsonSerializer.Deserialize<PayOsCreatePaymentResponse>(
                bodyText,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (data?.code == "00" && data.data?.checkoutUrl is { Length: > 0 } urlCheckout)
            {
                return PaymentResult.Success(urlCheckout);
            }

            return PaymentResult.Failed(
                data?.code ?? "PAYOS_NO_URL",
                data?.desc ?? "Không nhận được checkoutUrl từ payOS"
            );
        }
        catch (Exception ex)
        {
            return PaymentResult.Failed("PAYOS_ERROR", ex.Message);
        }
    }

    private static string SignHmacSha256(string raw, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var msgBytes = Encoding.UTF8.GetBytes(raw);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(msgBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
