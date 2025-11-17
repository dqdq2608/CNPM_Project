using eShop.EventBus.Abstractions;
using eShop.EventBus.Events;
using Microsoft.Extensions.Logging;
using Payment.IntegrationEvents.Events;
using WebhookReceiver.PayOS.Models;
using WebhookReceiver.PayOS.Services;

namespace WebhookReceiver.PayOS.Handlers;

public class PayOsWebhookHandler
{
    private readonly PayOsSignatureVerifier _verifier;
    private readonly IEventBus _eventBus;
    private readonly ILogger<PayOsWebhookHandler> _logger;

    public PayOsWebhookHandler(
        PayOsSignatureVerifier verifier,
        IEventBus eventBus,
        ILogger<PayOsWebhookHandler> logger)
    {
        _verifier = verifier;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(PayOsWebhookRequest body)
    {
        _logger.LogInformation("Received PayOS webhook: {@Body}", body);

        if (!_verifier.Verify(body))
        {
            _logger.LogWarning("Invalid PayOS signature. Webhook ignored.");
            return;
        }

        var data = body.Data;

        var isSuccess =
            body.Code == "00" &&
            body.Success &&
            data.Code == "00";

        var orderId = (int)data.OrderCode;

        _logger.LogInformation(
            "PayOS webhook parsed. OrderId={OrderId}, IsSuccess={IsSuccess}, Amount={Amount}",
            orderId,
            isSuccess,
            data.Amount);

        IntegrationEvent integrationEvent = isSuccess
            ? new OrderPaymentSucceededIntegrationEvent(orderId)
            : new OrderPaymentFailedIntegrationEvent(orderId);

        _logger.LogInformation(
            "Publishing integration event: {EventType} for OrderId={OrderId}",
            integrationEvent.GetType().Name,
            orderId);

        await _eventBus.PublishAsync(integrationEvent);
    }
}
