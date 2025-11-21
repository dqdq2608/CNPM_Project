using eShop.EventBus.Events;

namespace Payment.IntegrationEvents.Events;

public record OrderStartedIntegrationEvent : IntegrationEvent
{
    public string UserId { get; init; } = default!;
}
