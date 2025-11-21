using eShop.EventBus.Abstractions;
using eShop.EventBus.Events;
using Microsoft.Extensions.Logging;
using Payment.IntegrationEvents.Events;
using WebhookReceiver.PayOS.Models;
using WebhookReceiver.PayOS.Services;

namespace WebhookReceiver.PayOS.Handlers;

public class PayOsWebhookHandler : IPayOsWebhookHandler
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

    /// <summary>
    /// Xử lý webhook PayOS.
    /// skipSignature = true dùng để test local (bỏ qua verify chữ ký).
    /// </summary>
    public async Task HandleAsync(PayOsWebhookRequest? body, bool skipSignature = false)
    {
        if (body is null)
        {
            _logger.LogWarning("PayOS webhook body is null");
            return;
        }

        _logger.LogInformation("Received PayOS webhook: {@Body}", body);

        // verify chữ ký (trừ khi test có skipSignature=true)
        if (!skipSignature && !_verifier.Verify(body))
        {
            _logger.LogWarning("Invalid PayOS signature. Webhook ignored.");
            return;
        }

        var data = body.Data;
        if (data is null)
        {
            _logger.LogWarning("PayOS webhook data is null. Body={@Body}", body);
            return;
        }

        var isSuccess =
            string.Equals(body.Code, "00", StringComparison.OrdinalIgnoreCase) &&
            body.Success &&
            string.Equals(data.Code, "00", StringComparison.OrdinalIgnoreCase);

        // orderCode bên PayOS thường là long/int – ép sang int cho OrderId
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
