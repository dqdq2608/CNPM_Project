using eShop.EventBus.Abstractions;
using Microsoft.Extensions.Logging;
using Payment.IntegrationEvents.Events;
using PaymentProcessor.Apis;

namespace eShop.PaymentProcessor.IntegrationEvents.EventHandling;

public class OrderStatusChangedToStockConfirmedIntegrationEventHandler
    : IIntegrationEventHandler<OrderStatusChangedToStockConfirmedIntegrationEvent>
{
    private readonly IPaymentLinkService _paymentLinkService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<OrderStatusChangedToStockConfirmedIntegrationEventHandler> _logger;

    public OrderStatusChangedToStockConfirmedIntegrationEventHandler(
        IPaymentLinkService paymentLinkService,
        IEventBus eventBus,
        ILogger<OrderStatusChangedToStockConfirmedIntegrationEventHandler> logger)
    {
        _paymentLinkService = paymentLinkService;
        _eventBus           = eventBus;
        _logger             = logger;
    }

    public async Task Handle(OrderStatusChangedToStockConfirmedIntegrationEvent @event)
    {
        var amountVnd = @event.Total * 1000m;
        _logger.LogInformation(
            ">>> [HANDLER] Handling StockConfirmed event. EventId={EventId}, OrderId={OrderId}, Buyer={Buyer}, Total={Total}",
            @event.Id, @event.OrderId, @event.BuyerName, @event.Total);

        // Build thông tin thanh toán
        var description = $"Thanh toán đơn hàng {@event.OrderId}";
        var returnUrl   = "https://localhost:3000";
        var cancelUrl   = "https://localhost:3000";

        // Gọi service tạo link + cache
        var result = await _paymentLinkService.CreateAndCachePaymentLinkAsync(
            orderId:   @event.OrderId,
            amount:    amountVnd,
            description,
            returnUrl,
            cancelUrl);

        _logger.LogInformation(
            ">>> [HANDLER] Payment service result for OrderId {OrderId}: IsSuccess={IsSuccess}, Url={Url}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
            @event.OrderId, result.IsSuccess, result.PaymentUrl, result.ErrorCode, result.ErrorMessage);

        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.PaymentUrl))
        {
            var failedEvt = new OrderPaymentFailedIntegrationEvent(@event.OrderId);

            _logger.LogWarning(
                ">>> [HANDLER] Payment failed. Publishing OrderPaymentFailedIntegrationEvent for OrderId {OrderId}",
                @event.OrderId);

            await _eventBus.PublishAsync(failedEvt);
            return;
        }

        _logger.LogInformation(
            ">>> [HANDLER] Payment link created & cached for OrderId {OrderId}. Url={Url}",
            @event.OrderId, result.PaymentUrl);
    }
}
