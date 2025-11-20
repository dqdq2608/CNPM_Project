using System;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerBFF.Application.Services;

public interface IPaymentApi
{
    /// <summary>
    /// Dùng cho endpoint /checkoutonline ở BFF.
    /// Nhận body JSON từ frontend (React), tự lo:
    ///  - Gọi Ordering.API để tạo Order
    ///  - (Nếu cần) Gọi PaymentProcessor để tạo link PayOS
    ///  - Trả về JSON chứa OrderId, Total, PaymentUrl...
    /// </summary>
    /// <param name="bodyJson">
    /// JSON gửi từ frontend. Bạn có thể dùng đúng schema CreateOrderRequest
    /// (UserId, UserName, Address, Card..., Items)
    /// </param>
    /// <param name="cancellationToken"></param>
    Task<string> CheckoutOnlineAsync(
        string bodyJson,
        CancellationToken cancellationToken = default);
}
