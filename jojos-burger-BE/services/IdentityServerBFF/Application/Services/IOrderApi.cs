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

    Task<string> GetOrdersForUserAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default
    );

    Task<string> GetOrderDetailAsync(
        ClaimsPrincipal user,
        int orderId,
        CancellationToken cancellationToken = default
    );
}
