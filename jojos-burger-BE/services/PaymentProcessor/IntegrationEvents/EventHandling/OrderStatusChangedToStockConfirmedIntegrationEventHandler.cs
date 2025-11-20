using eShop.EventBus.Abstractions;
using eShop.EventBus.Events;
using Microsoft.Extensions.Logging;
using Payment.IntegrationEvents.Events;
using PaymentProcessor.Apis; // 👈 dùng IPaymentLinkService thay cho IPaymentProvider

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
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(OrderStatusChangedToStockConfirmedIntegrationEvent @event)
    {
        _logger.LogInformation(
            ">>> [HANDLER] Handling OrderStatusChangedToStockConfirmedIntegrationEvent. EventId={EventId}, OrderId={OrderId}, Buyer={Buyer}, Total={Total}",
            @event.Id, @event.OrderId, @event.BuyerName, @event.Total);

        // Gọi service tạo link + lưu cache (in-memory)
        var result = await _paymentLinkService.CreateAndCachePaymentLinkAsync(
            orderId: @event.OrderId,
            amount: @event.Total,
            description: $"Thanh toán đơn hàng {@event.OrderId}",
            returnUrl: "https://example.com/payment/success",
            cancelUrl: "https://example.com/payment/cancel");

        _logger.LogInformation(
            ">>> [HANDLER] Payment link result for OrderId {OrderId}: IsSuccess={IsSuccess}, Url={Url}, IsNewLink={IsNewLink}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
            @event.OrderId, result.IsSuccess, result.PaymentUrl, result.IsNewLink, result.ErrorCode, result.ErrorMessage);

        if (!result.IsSuccess)
        {
            // ❌ Tạo link thất bại ⇒ xem như thanh toán fail luôn
            var failedEvt = new OrderPaymentFailedIntegrationEvent(@event.OrderId);

            _logger.LogInformation(
                ">>> [HANDLER] Publishing OrderPaymentFailedIntegrationEvent for OrderId {OrderId}",
                @event.OrderId);

            await _eventBus.PublishAsync(failedEvt);
            return;
        }

        // ✅ Tạo link thành công: link đã được cache trong IPaymentLinkCache
        // FE/BFF sẽ gọi API khác (vd: GET /api/payments/{orderId}) để lấy paymentUrl và redirect.
        _logger.LogInformation(
            ">>> [HANDLER] Payment link cached for OrderId {OrderId}. Client can later redirect to: {Url}",
            @event.OrderId, result.PaymentUrl);
    }
}
