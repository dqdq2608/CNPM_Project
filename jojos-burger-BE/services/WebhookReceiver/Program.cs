var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Handshake: echo x-eshop-whtoken
app.MapMethods("/webhooks/eshop", new[] { "OPTIONS" }, (HttpRequest req, HttpResponse res) =>
{
    if (!req.Headers.TryGetValue("x-eshop-whtoken", out var token) || string.IsNullOrWhiteSpace(token))
    { res.StatusCode = 400; return res.WriteAsync(@"{""error"":""missing x-eshop-whtoken""}"); }
    res.Headers["x-eshop-whtoken"] = token.ToString();
    res.ContentType = "application/json"; res.StatusCode = 200;
    return res.WriteAsync(@"{""ok"":true,""type"":""grant""}");
});

// Nhận sự kiện thật (POST) + (tuỳ chọn) verify HMAC
app.MapPost("/webhooks/eshop", async (HttpRequest req, IConfiguration cfg, ILoggerFactory lf) =>
{
    var log = lf.CreateLogger("WebhookReceiver");
    var secret = cfg["Webhooks:Secret"];                // đặt qua env nếu dùng HMAC
    var signature = req.Headers["X-Signature"].ToString();

    using var sr = new StreamReader(req.Body, leaveOpen:true);
    var raw = await sr.ReadToEndAsync();

    if (!string.IsNullOrWhiteSpace(secret) && !string.IsNullOrWhiteSpace(signature))
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = Convert.ToHexString(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        if (!signature.Equals($"sha256={hash}", StringComparison.OrdinalIgnoreCase))
            return Results.Unauthorized();
    }

    log.LogInformation("Webhook received: {Payload}", raw);
    // TODO: lưu DB / publish MQ / gọi BFF để push SignalR / gọi Services khác
    return Results.Ok(new { ok = true });
});

app.MapGet("/health", () => Results.Ok(new { ok = true, svc = "WebhookReceiver" }));
app.Run("http://0.0.0.0:6105");
