namespace Payment.Providers.Momo.Models;

public class PaymentRequest
{
    public string partnerCode { get; set; } = default!;
    public string partnerName { get; set; } = "MoMo";
    public string storeId     { get; set; } = "Store01";

    public string requestId   { get; set; } = default!;
    public string amount      { get; set; } = default!;
    public string orderId     { get; set; } = default!;
    public string orderInfo   { get; set; } = default!;

    public string redirectUrl { get; set; } = default!;
    public string ipnUrl      { get; set; } = default!;
    public string extraData   { get; set; } = "";

    public string requestType { get; set; } = "captureWallet";
    public string signature   { get; set; } = default!;
}
