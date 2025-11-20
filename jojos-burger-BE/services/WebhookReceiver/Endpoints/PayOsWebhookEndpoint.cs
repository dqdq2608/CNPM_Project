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
            [FromQuery] bool? skipSignature,           // cho phép null
            [FromServices] IPayOsWebhookHandler handler) =>
        {
            // Khi test local:  ?skipSignature=true  -> bỏ verify chữ ký
            // Khi PayOS gọi thật: không có query   -> verify chữ ký bình thường
            var ignoreSignature = skipSignature == true;

            await handler.HandleAsync(body, ignoreSignature);
            return Results.Ok();
        });

        return endpoints;
    }
}
