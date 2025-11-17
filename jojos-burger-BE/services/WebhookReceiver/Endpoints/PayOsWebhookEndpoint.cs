using Microsoft.AspNetCore.Mvc;
using WebhookReceiver.PayOS.Handlers;
using WebhookReceiver.PayOS.Models;

namespace WebhookReceiver.PayOS.Endpoints;

public static class PayOsWebhookEndpoint
{
    public static IEndpointRouteBuilder MapPayOsWebhook(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/webhooks/payos", async (
            [FromBody] PayOsWebhookRequest body,
            PayOsWebhookHandler handler) =>
        {
            await handler.HandleAsync(body);
            return Results.Ok();  // cho PayOS biết đã nhận
        });

        return endpoints;
    }
}
