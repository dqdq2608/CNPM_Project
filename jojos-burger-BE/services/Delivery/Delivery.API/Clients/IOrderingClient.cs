namespace Delivery.API.Clients;

public interface IOrderingClient
{
    Task MarkOrderDeliveredAsync(int orderId, CancellationToken cancellationToken = default);
}
