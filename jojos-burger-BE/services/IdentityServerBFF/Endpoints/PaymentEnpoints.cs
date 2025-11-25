using System.Text;
using System.Text.Json;
using IdentityServerBFF.Application.Services;
using Microsoft.AspNetCore.Http;

namespace IdentityServerBFF.Endpoints
{
    public static class CheckoutEndpoints
    {
        public static RouteGroupBuilder MapCheckoutEndpoints(this RouteGroupBuilder group)
        {
            var checkout = group.MapGroup("/checkoutonline")
                                .RequireAuthorization(); // bắt buộc đã login

            // POST /api/checkoutonline
            checkout.MapPost("/", async (
                HttpContext ctx,
                IPaymentApi api,
                CancellationToken ct) =>
            {
                // 1. Lấy userId từ claim (thường là "sub")
                var user = ctx.User;
                var sub = user.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(sub))
                {
                    return Results.Unauthorized();
                }

                // 2. Đọc body gốc từ FE
                using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8);
                var bodyJson = await reader.ReadToEndAsync(ct);

                // 3. Parse JSON và merge thêm userId
                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(bodyJson) ? "{}" : bodyJson);
                var root = doc.RootElement;

                using var ms = new MemoryStream();
                using (var writer = new Utf8JsonWriter(ms))
                {
                    writer.WriteStartObject();

                    // copy tất cả prop cũ trừ userId/UserId (nếu FE có gửi thì mình override)
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.NameEquals("userId") || prop.NameEquals("UserId"))
                            continue;

                        prop.WriteTo(writer);
                    }

                    // ghi thêm userId từ claim
                    writer.WriteString("userId", sub);

                    writer.WriteEndObject();
                }

                ms.Position = 0;
                var mergedBodyJson = Encoding.UTF8.GetString(ms.ToArray());

                // 4. Gọi xuống Ordering qua IPaymentApi với body đã có userId
                var json = await api.CheckoutOnlineAsync(mergedBodyJson, ct);

                return Results.Content(json, "application/json");
            });

            return checkout;
        }
    }

    // ================================
    //  PaymentEndpoints: GET /api/payments/{orderId}
    // ================================
    public static class PaymentEndpoints
    {
        public static RouteGroupBuilder MapPaymentEndpoints(this RouteGroupBuilder group)
        {
            var payment = group.MapGroup("/payments")
                            .RequireAuthorization();

            payment.MapGet("/{orderId:int}", async (
                int orderId,
                IHttpClientFactory httpClientFactory,
                CancellationToken ct) =>
            {
                var client = httpClientFactory.CreateClient("kong");

                var resp = await client.GetAsync($"/api/payments/{orderId}", ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                // Forward đúng status code từ PaymentProcessor
                if (!resp.IsSuccessStatusCode)
                {
                    return Results.StatusCode((int)resp.StatusCode);
                }

                // 200 và có body => trả luôn JSON
                return Results.Content(body, "application/json");
            });

            return payment;
        }
    }
}
