namespace WebhookReceiver.PayOS.Models;

// Map theo WebhookType trong docs payOS
public class PayOsWebhookRequest
{
    public string Code { get; set; } = default!;      // "00" = ok
    public string Desc { get; set; } = default!;
    public bool Success { get; set; }
    public PayOsWebhookData Data { get; set; } = default!;
    public string Signature { get; set; } = default!;
}
