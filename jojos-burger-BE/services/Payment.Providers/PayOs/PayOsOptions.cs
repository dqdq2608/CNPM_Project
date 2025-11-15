namespace Payment.Providers.PayOS;

public class PayOsOptions
{
    public const string SectionName = "PayOS";

    public string ClientId { get; set; } = default!;
    public string ApiKey { get; set; } = default!;
    public string ChecksumKey { get; set; } = default!;

    // Không bắt buộc, dùng nếu tham gia chương trình Đối tác tích hợp
    public string? PartnerCode { get; set; }

    // BaseUrl nếu sau này PayOS đổi, hiện mặc định là:
    // https://api-merchant.payos.vn
    public string BaseUrl { get; set; } = "https://api-merchant.payos.vn";
}
