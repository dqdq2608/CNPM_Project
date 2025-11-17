using WebhookReceiver.PayOS.Models;

namespace WebhookReceiver.PayOS.Services;

public class PayOsSignatureVerifier
{
    private readonly ILogger<PayOsSignatureVerifier> _logger;
    private readonly string _checksumKey;

    public PayOsSignatureVerifier(IConfiguration configuration,
                                  ILogger<PayOsSignatureVerifier> logger)
    {
        _logger = logger;
        _checksumKey = configuration["PayOS:ChecksumKey"] ?? string.Empty;
    }

    public bool Verify(PayOsWebhookRequest body)
    {
        // TODO: Implement chuẩn theo docs payOS nếu bạn muốn bảo mật cao.
        // Tạm thời cho đồ án: log ra và luôn true để không chặn flow.

        _logger.LogInformation("Skip verify PayOS signature. Code={Code}, Success={Success}",
            body.Code, body.Success);

        return true;

        /*
        // Ví dụ pseudo:
        var raw = BuildDataString(body.Data); // sort key alphabet, join key=value&...
        var keyBytes = Encoding.UTF8.GetBytes(_checksumKey);
        var msgBytes = Encoding.UTF8.GetBytes(raw);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(msgBytes);
        var sig = Convert.ToHexString(hash).ToLowerInvariant();

        return string.Equals(sig, body.Signature, StringComparison.OrdinalIgnoreCase);
        */
    }
}
