using Payment.Providers.Abstractions;
using PaymentProcessor.Apis;

namespace PaymentProcessor.Apis;

public class PaymentApi : IPaymentApi
{
    private readonly IPaymentProvider _paymentProvider;

    public PaymentApi(IPaymentProvider paymentProvider)
    {
        _paymentProvider = paymentProvider;
    }

    public async Task<PaymentLinkResponse> CreatePaymentAsync(CreatePaymentRequest request)
    {
        var orderData = new OrderPaymentData
        {
            OrderId     = request.OrderId.ToString(),
            Amount      = request.Amount,
            Description = request.Description ?? $"Thanh toán đơn hàng {request.OrderId}",
            ReturnUrl   = request.ReturnUrl,
            CancelUrl   = request.CancelUrl
        };

        var result = await _paymentProvider.CreatePaymentAsync(orderData);

        return new PaymentLinkResponse
        {
            IsSuccess    = result.IsSuccess,
            PaymentUrl   = result.PaymentUrl,
            ErrorCode    = result.ErrorCode,
            ErrorMessage = result.ErrorMessage
        };
    }
}
