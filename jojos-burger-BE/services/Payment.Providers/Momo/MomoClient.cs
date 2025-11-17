using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Payment.Providers.Abstractions;
using Payment.Providers.Momo.Models;

namespace Payment.Providers.Momo;

public class MomoClient
{
    private readonly HttpClient   _httpClient;
    private readonly MomoOptions  _options;

    public MomoClient(HttpClient httpClient, IOptions<MomoOptions> options)
    {
        _httpClient = httpClient;
        _options    = options.Value;
    }

    public async Task<PaymentResult> CreatePaymentAsync(OrderPaymentData order)
    {
        // 1. Build request
        var requestId = Guid.NewGuid().ToString("N");
        var orderId   = order.OrderId;

        var returnUrl = order.ReturnUrl ?? _options.RedirectUrl;
        var ipnUrl    = order.NotifyUrl ?? _options.IpNotifyUrl;

        var amount = ((long)order.Amount).ToString(); // MoMo dùng string số nguyên
        var orderInfo = order.Description ?? $"Thanh toán đơn hàng {orderId}";

        var rawSignature =
            $"accessKey={_options.AccessKey}" +
            $"&amount={amount}" +
            $"&extraData=" +
            $"&ipnUrl={ipnUrl}" +
            $"&orderId={orderId}" +
            $"&orderInfo={order.Description}" +
            $"&partnerCode={_options.PartnerCode}" +
            $"&redirectUrl={returnUrl}" +
            $"&requestId={requestId}" +
            $"&requestType=captureWallet";

        var signature = MomoSignatureHelper.SignHmacSha256(rawSignature, _options.SecretKey);

        var paymentRequest = new PaymentRequest
        {
            partnerCode = _options.PartnerCode,
            requestId   = requestId,
            amount      = amount,
            orderId     = orderId,
            orderInfo   = order.Description ?? $"Thanh toán đơn hàng {orderId}",
            redirectUrl = returnUrl,
            ipnUrl      = ipnUrl,
            extraData   = "",          // có thể encode thêm info nếu cần
            signature   = signature
        };

        // 2. Gửi request tới MoMo
        using var httpResponse = await _httpClient.PostAsJsonAsync(_options.Endpoint, paymentRequest);
        var response = await httpResponse.Content.ReadFromJsonAsync<PaymentResponse>()
                      ?? new PaymentResponse { resultCode = -1, message = "Empty response from MoMo" };

        // 3. Xử lý kết quả
        if (response.resultCode == 0)
        {
            // OK, trả về URL để FE redirect
            var payUrl = response.payUrl ?? response.deeplink;
            return PaymentResult.Success(payUrl);
        }

        return PaymentResult.Failed(response.resultCode.ToString(), response.message);
    }
}
