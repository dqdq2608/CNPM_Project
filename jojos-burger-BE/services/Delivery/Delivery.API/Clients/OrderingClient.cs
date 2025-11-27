using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Delivery.API.Clients;

public class OrderingClient : IOrderingClient
{
    private readonly HttpClient _httpClient;

    public OrderingClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task MarkOrderDeliveredAsync(int orderId, CancellationToken cancellationToken = default)
    {
        // Gọi đúng endpoint nội bộ mà ta đã tạo bên Ordering:
        // POST /api/internal/orders/{orderId}/mark-delivered
        var response = await _httpClient.PostAsync(
            $"/api/internal/orders/{orderId}/mark-delivered",
            content: null,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
