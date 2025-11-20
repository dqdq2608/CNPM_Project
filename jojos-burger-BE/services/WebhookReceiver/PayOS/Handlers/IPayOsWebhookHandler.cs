using WebhookReceiver.PayOS.Models;

namespace WebhookReceiver.PayOS.Handlers;

public interface IPayOsWebhookHandler
{
    Task HandleAsync(PayOsWebhookRequest body, bool skipSignature = false);
}
