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

    Task<string> TickDeliveryAsync(
        ClaimsPrincipal user,
        int orderId,
        CancellationToken cancellationToken = default
    );

    Task ConfirmDeliveryAsync(
        ClaimsPrincipal user,
        int orderId,
        CancellationToken cancellationToken = default
    );

    Task<string> GetOrderDetailAsync(
        ClaimsPrincipal user,
        int orderId,
        CancellationToken cancellationToken = default
    );

    Task<string> GetRestaurantOrdersAsync(ClaimsPrincipal user, CancellationToken ct = default);
    Task StartDeliveryAsync(int orderId, CancellationToken ct = default);

    Task<string> GetDeliveryForOrderAsync(ClaimsPrincipal user, int orderId, CancellationToken cancellationToken = default);

    Task<DeliveryQuoteResponse> GetDeliveryQuoteAsync(
    ClaimsPrincipal user,
    DeliveryQuoteRequest request,
    CancellationToken cancellationToken = default);

}
