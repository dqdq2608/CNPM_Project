namespace Payment.Providers.Momo.Models;

public class PaymentResponse
{
    public int resultCode { get; set; }
    public string message { get; set; } = default!;
    public string? payUrl { get; set; }
    public string? deeplink { get; set; }
}
