using eShop.EventBus.Abstractions;
using Microsoft.Extensions.Logging;
using Payment.IntegrationEvents.Events;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using OrderPaymentSucceededIntegrationEvent = Payment.IntegrationEvents.Events.OrderPaymentSucceededIntegrationEvent;

namespace eShop.Ordering.API.IntegrationEvents.EventHandling;

public class OrderPaymentSucceededIntegrationEventHandler
    : IIntegrationEventHandler<OrderPaymentSucceededIntegrationEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderPaymentSucceededIntegrationEventHandler> _logger;

    public OrderPaymentSucceededIntegrationEventHandler(
        IOrderRepository orderRepository,
        ILogger<OrderPaymentSucceededIntegrationEventHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task Handle(OrderPaymentSucceededIntegrationEvent @event)
    {
        _logger.LogInformation(
            ">>> [ORDERING] Handling OrderPaymentSucceededIntegrationEvent. OrderId={OrderId}",
            @event.OrderId);

        var order = await _orderRepository.GetAsync(@event.OrderId);
        if (order is null)
        {
            _logger.LogWarning(
                ">>> [ORDERING] Order not found when handling OrderPaymentSucceededIntegrationEvent. OrderId={OrderId}",
                @event.OrderId);
            return;
        }

        order.SetPaidStatus();

        await _orderRepository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation(
            ">>> [ORDERING] Order {OrderId} set to PAID successfully.",
            @event.OrderId);
    }
}
