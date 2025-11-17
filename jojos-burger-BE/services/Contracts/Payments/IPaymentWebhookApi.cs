// Contracts/Payments/IPaymentWebhookApi.cs
namespace Contracts.Payments;

public interface IPaymentWebhookApi
{
    // FE / BFF gọi để tạo link thanh toán
    Task<PaymentLinkResponse> CreatePaymentAsync(CreatePaymentRequest request);

    // PayOS gọi webhook để báo kết quả thanh toán
    Task<WebhookAcknowledge> ReceiveWebhookAsync(PayOsWebhookRequest payload);
}

// DTOs tối giản
public class CreatePaymentRequest
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string ReturnUrl { get; set; } = default!;
    public string CancelUrl { get; set; } = default!;
}

public class PaymentLinkResponse
{
    public bool IsSuccess { get; set; }
    public string? PaymentUrl { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public class WebhookAcknowledge
{
    public bool Success { get; set; }
}
