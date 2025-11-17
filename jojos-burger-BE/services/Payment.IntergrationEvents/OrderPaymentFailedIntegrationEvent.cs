using eShop.EventBus.Events;
namespace Payment.IntegrationEvents.Events;

public record OrderPaymentFailedIntegrationEvent(int OrderId) : IntegrationEvent;
