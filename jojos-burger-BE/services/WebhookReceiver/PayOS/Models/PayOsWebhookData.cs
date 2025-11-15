namespace WebhookReceiver.PayOS.Models;

// Map theo WebhookDataType trong docs payOS
public class PayOsWebhookData
{
    public long OrderCode { get; set; }
    public int Amount { get; set; }
    public string Description { get; set; } = default!;
    public string AccountNumber { get; set; } = default!;
    public string Reference { get; set; } = default!;
    public string TransactionDateTime { get; set; } = default!;
    public string Currency { get; set; } = default!;
    public string PaymentLinkId { get; set; } = default!;
    public string Code { get; set; } = default!;  // "00" = success
    public string Desc { get; set; } = default!;

    // Các field dưới đây có thể null tuỳ ngân hàng
    public string? CounterAccountBankId { get; set; }
    public string? CounterAccountBankName { get; set; }
    public string? CounterAccountName { get; set; }
    public string? CounterAccountNumber { get; set; }
    public string? VirtualAccountNumber { get; set; }
    public string? VirtualAccountName { get; set; }
}
