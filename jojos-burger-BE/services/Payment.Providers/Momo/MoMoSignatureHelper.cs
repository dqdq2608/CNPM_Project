using System.Security.Cryptography;
using System.Text;

namespace Payment.Providers.Momo;

public static class MomoSignatureHelper
{
    /// <summary>
    /// Tạo chữ ký HMAC SHA256 cho chuỗi rawSignature theo yêu cầu của MoMo.
    /// </summary>
    public static string SignHmacSha256(string rawSignature, string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var messageBytes = Encoding.UTF8.GetBytes(rawSignature);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(messageBytes);

#if NET8_0_OR_GREATER
        // .NET 8 có sẵn Convert.ToHexString
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
#else
        var hex = BitConverter.ToString(hashBytes).Replace("-", "");
        return hex.ToLowerInvariant();
#endif
    }

    /// <summary>
    /// Verify chữ ký HMAC SHA256 từ MoMo gửi về (webhook).
    /// </summary>
    public static bool VerifyHmacSha256(string rawSignature, string secretKey, string momoSignature)
    {
        var localSig = SignHmacSha256(rawSignature, secretKey);
        return string.Equals(localSig, momoSignature, StringComparison.OrdinalIgnoreCase);
    }
}
