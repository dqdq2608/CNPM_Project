using Payment.Providers.Abstractions;
using eShop.EventBus.Abstractions;
using eShop.EventBus.Events;
using Microsoft.Extensions.Logging;
using Payment.IntegrationEvents.Events;

namespace eShop.PaymentProcessor.IntegrationEvents.EventHandling;
public class OrderStatusChangedToStockConfirmedIntegrationEventHandler(
    IPaymentProvider paymentProvider,
    IEventBus eventBus,
    ILogger<OrderStatusChangedToStockConfirmedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<OrderStatusChangedToStockConfirmedIntegrationEvent>
{
    public async Task Handle(OrderStatusChangedToStockConfirmedIntegrationEvent @event)
    {
        logger.LogInformation("Handling event {EventId}", @event.Id);

        var orderData = new OrderPaymentData
        {
            OrderId     = @event.OrderId.ToString(),
            Amount      = 10_000m, // tạm hard-code, sau này lấy từ Order
            Description = $"Thanh toán đơn hàng {@event.OrderId}",
            ReturnUrl   = "https://your-frontend.com/payment/success",
            CancelUrl   = "https://your-frontend.com/payment/cancel"
        };

        var result = await paymentProvider.CreatePaymentAsync(orderData);

        IntegrationEvent evt;
        if (result.IsSuccess)
            evt = new OrderPaymentSucceededIntegrationEvent(@event.OrderId);
        else
            evt = new OrderPaymentFailedIntegrationEvent(@event.OrderId);

        await eventBus.PublishAsync(evt);
    }
}
