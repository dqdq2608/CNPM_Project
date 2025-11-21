using eShop.EventBus.Events;

namespace Payment.IntegrationEvents.Events;

public record OrderStatusChangedToSubmittedIntegrationEvent : IntegrationEvent
{
    public int    OrderId          { get; init; }
    public string OrderStatus      { get; init; } = default!;
    public string BuyerName        { get; init; } = default!;
    public string BuyerIdentityGuid{ get; init; } = default!;
}
