namespace Payment.Providers.Momo;

public class MomoOptions
{
    public const string SectionName = "Momo";

    public string PartnerCode { get; set; } = default!;
    public string AccessKey  { get; set; } = default!;
    public string SecretKey  { get; set; } = default!;
    public string Endpoint   { get; set; } = default!;  // https://test-payment.momo.vn/v2/gateway/api/create

    /// <summary>
    /// URL FE để redirect sau khi thanh toán (returnUrl).
    /// </summary>
    public string RedirectUrl { get; set; } = default!;

    /// <summary>
    /// URL BE để MoMo gọi IPN (ipnUrl).
    /// </summary>
    public string IpNotifyUrl { get; set; } = default!;
}
