namespace PaymentProcessor.Apis;

public interface IPaymentApi
{
    Task<PaymentLinkResponse> CreatePaymentAsync(CreatePaymentRequest request);
}

// DTOs
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
