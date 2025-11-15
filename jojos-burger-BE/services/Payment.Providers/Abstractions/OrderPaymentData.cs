namespace Payment.Providers.Abstractions;

public class OrderPaymentData
{
    public string OrderId { get; set; } = default!;
    public decimal Amount { get; set; }

    public string? Description { get; set; }

    // URL FE nhận kết quả thành công
    public string? ReturnUrl { get; set; }

    // URL FE nhận khi khách bấm Huỷ
    public string? CancelUrl { get; set; }

    public string? NotifyUrl { get; set; }
    public string? PaymentMethod { get; set; }
}
