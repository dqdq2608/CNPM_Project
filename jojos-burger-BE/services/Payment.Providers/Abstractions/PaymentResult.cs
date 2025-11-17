namespace Payment.Providers.Abstractions;

public class PaymentResult
{
    public bool IsSuccess { get; init; }
    public string? PaymentUrl { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static PaymentResult Success(string? paymentUrl = null)
        => new()
        {
            IsSuccess = true,
            PaymentUrl = paymentUrl
        };

    public static PaymentResult Failed(string? errorCode, string? errorMessage)
        => new()
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
}
