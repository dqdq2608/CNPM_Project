using eShop.EventBus.Abstractions;
using Microsoft.Extensions.Logging;
using Payment.IntegrationEvents.Events;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using OrderPaymentFailedIntegrationEvent = Payment.IntegrationEvents.Events.OrderPaymentFailedIntegrationEvent;

namespace eShop.Ordering.API.IntegrationEvents.EventHandling;

public class OrderPaymentFailedIntegrationEventHandler
    : IIntegrationEventHandler<OrderPaymentFailedIntegrationEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderPaymentFailedIntegrationEventHandler> _logger;

    public OrderPaymentFailedIntegrationEventHandler(
        IOrderRepository orderRepository,
        ILogger<OrderPaymentFailedIntegrationEventHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task Handle(OrderPaymentFailedIntegrationEvent @event)
    {
        _logger.LogInformation(
            ">>> [ORDERING] Handling OrderPaymentFailedIntegrationEvent. OrderId={OrderId}",
            @event.OrderId);

        var order = await _orderRepository.GetAsync(@event.OrderId);
        if (order is null)
        {
            _logger.LogWarning(
                ">>> [ORDERING] Order not found when handling OrderPaymentFailedIntegrationEvent. OrderId={OrderId}",
                @event.OrderId);
            return;
        }

        // Tuỳ nghiệp vụ: huỷ đơn hoặc set trạng thái PaymentFailed
        order.SetCancelledStatus(); // hoặc tên method domain của bạn

        await _orderRepository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation(
            ">>> [ORDERING] Order {OrderId} set to PAYMENT FAILED / CANCELLED.",
            @event.OrderId);
    }
}
