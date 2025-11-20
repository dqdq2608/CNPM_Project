using eShop.EventBus.Events;

namespace eShop.Ordering.API.Application.IntegrationEvents.Events;

public record OrderStatusChangedToStockConfirmedIntegrationEvent : IntegrationEvent
{
    public int    OrderId           { get; init; }
    public string OrderStatus       { get; init; } = default!;
    public string BuyerName         { get; init; } = default!;
    public string BuyerIdentityGuid { get; init; } = default!;
    public decimal Total { get; set; }
}
