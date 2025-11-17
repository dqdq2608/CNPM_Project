namespace Payment.Providers.Momo.Models;

public class QuickPayRequest
{
    public string orderInfo { get; set; } = default!;
    public string partnerCode { get; set; } = default!;
    public string redirectUrl { get; set; } = default!;
    public string ipnUrl { get; set; } = default!;
    public long amount { get; set; }
    public string orderId { get; set; } = default!;
    public string requestId { get; set; } = default!;
    public string extraData { get; set; } = "";
    public string partnerName { get; set; } = "MoMo";
    public string storeId { get; set; } = "Store01";
    public string? paymentCode { get; set; }
    public string? orderGroupId { get; set; }
    public bool autoCapture { get; set; } = true;
    public string lang { get; set; } = "vi";
    public string signature { get; set; } = default!;
}
