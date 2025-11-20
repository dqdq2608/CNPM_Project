using eShop.EventBus.Abstractions;
using eShop.Ordering.API.Application.IntegrationEvents.Events;
using eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace eShop.Ordering.API.Application.DomainEventHandlers;

public class OrderStatusChangedToPaidDomainEventHandler
    : INotificationHandler<OrderStatusChangedToPaidDomainEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IBuyerRepository _buyerRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<OrderStatusChangedToPaidDomainEventHandler> _logger;

    public OrderStatusChangedToPaidDomainEventHandler(
        IOrderRepository orderRepository,
        IBuyerRepository buyerRepository,
        IEventBus eventBus,
        ILogger<OrderStatusChangedToPaidDomainEventHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _buyerRepository = buyerRepository ?? throw new ArgumentNullException(nameof(buyerRepository));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(OrderStatusChangedToPaidDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // log trạng thái
        OrderingApiTrace.LogOrderStatusUpdated(_logger, domainEvent.OrderId, OrderStatus.Paid);

        // lấy thêm thông tin order + buyer (nếu cần cho event)
        var order = await _orderRepository.GetAsync(domainEvent.OrderId);
        var buyer = await _buyerRepository.FindByIdAsync(order.BuyerId!.Value);

        var orderStockList = domainEvent.OrderItems
            .Select(orderItem => new OrderStockItem(orderItem.ProductId, orderItem.Units));

        var integrationEvent = new OrderStatusChangedToPaidIntegrationEvent(
            domainEvent.OrderId,
            order.OrderStatus,
            buyer.Name,
            buyer.IdentityGuid,
            orderStockList);

        _logger.LogInformation(
            ">>> [ORDERING] Publishing OrderStatusChangedToPaidIntegrationEvent for OrderId={OrderId}",
            domainEvent.OrderId);

        // ✅ Publish thẳng ra EventBus, KHÔNG đụng tới IntegrationEventLog / transaction nữa
        await _eventBus.PublishAsync(integrationEvent);
    }
}
