using WebhookReceiver.PayOS.Handlers;
using WebhookReceiver.PayOS.Models;

namespace WebhookReceiver.Endpoints;

public static class PayOsWebhookEndpoint
{
    public static IEndpointRouteBuilder MapPayOsWebhook(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/webhooks/payos", async (
            PayOsWebhookRequest body,
            PayOsWebhookHandler handler) =>
        {
            await handler.HandleAsync(body);

            // Trả về 200 để PayOS coi như webhook thành công
            return Results.Ok(new { success = true });
        });

        return endpoints;
    }
}
