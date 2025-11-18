using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerBFF.Application.Services;

public interface IOrderBffApi
{
    // Tạo order từ giỏ hiện tại của user (dùng Basket + Ordering)
    Task<string> CreateOrderFromBasketAsync(
        ClaimsPrincipal user,
        FrontCreateOrderRequest request,
        CancellationToken cancellationToken = default
    );

    // Sau này bạn có thể thêm:
    // Task<string> GetOrdersForUserAsync(ClaimsPrincipal user, CancellationToken ct = default);
    // Task<string> GetOrderByIdAsync(ClaimsPrincipal user, int orderId, CancellationToken ct = default);
}
