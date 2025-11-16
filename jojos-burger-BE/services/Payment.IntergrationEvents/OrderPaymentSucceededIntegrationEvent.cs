using eShop.EventBus.Events;
namespace Payment.IntegrationEvents.Events;

public record OrderPaymentSucceededIntegrationEvent(int OrderId) : IntegrationEvent;