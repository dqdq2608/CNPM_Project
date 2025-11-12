using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;



var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("pub").ConfigurePrimaryHttpMessageHandler(() =>
    new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator });

var app  = builder.Build();
var regs = new ConcurrentDictionary<Guid, Registration>();

app.MapGet("/health", () => Results.Ok(new { ok = true, svc = "WebhookPublisher" }));

app.MapPost("/api/webhooks/registrations", async (IHttpClientFactory f, Registration reg) =>
{
    var http  = f.CreateClient("pub");
    var token = Guid.NewGuid().ToString("N");

    var req = new HttpRequestMessage(HttpMethod.Options, reg.GrantUrl);
    req.Headers.TryAddWithoutValidation("x-eshop-whtoken", token);
    var res = await http.SendAsync(req);
    if (!res.IsSuccessStatusCode) return Results.Problem($"Grant failed: {(int)res.StatusCode}");
    if (!res.Headers.TryGetValues("x-eshop-whtoken", out var vals) || vals.FirstOrDefault() != token)
        return Results.Problem("Grant failed: token echo mismatch");

    reg.Id = Guid.NewGuid();
    regs[reg.Id.Value] = reg;
    return Results.Ok(reg);
});

app.MapGet("/api/webhooks/registrations", () => Results.Ok(regs.Values));

app.MapDelete("/api/webhooks/registrations/{id:guid}", (Guid id) =>
{
    regs.TryRemove(id, out _);
    return Results.NoContent();
});

app.MapPost("/simulate/order-submitted", async (IHttpClientFactory f, JsonElement payload) =>
{
    var http = f.CreateClient("pub");
    var body = JsonSerializer.Serialize(new {
        eventType  = "OrderStatusChangedToSubmitted",
        occurredOn = DateTime.UtcNow,
        data       = payload
    });

    var tasks   = regs.Values.Select(reg => Send(http, reg, body)); // <-- local function
    var results = await Task.WhenAll(tasks);
    return Results.Ok(results);
});

// ---- local function phải NẰM TRƯỚC mọi type, và TRƯỚC app.Run cũng được ----
async Task<object> Send(HttpClient http, Registration reg, string body)
{
    using var msg = new HttpRequestMessage(HttpMethod.Post, reg.HandlerUrl);
    msg.Content = new StringContent(body, Encoding.UTF8, "application/json");

    if (!string.IsNullOrWhiteSpace(reg.Token))
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(reg.Token));
        var sig = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        msg.Headers.TryAddWithoutValidation("X-Signature", $"sha256={sig}");
    }

    var res  = await http.SendAsync(msg);
    var text = await res.Content.ReadAsStringAsync();
    return new { reg.Id, StatusCode = (int)res.StatusCode, Body = text };
}

app.Run("http://0.0.0.0:6110");

// ---- type declarations (record/class/namespace) PHẢI ở CUỐI file, sau app.Run ----
record Registration
{
    public Guid?  Id         { get; set; }
    public string GrantUrl   { get; init; } = default!;
    public string HandlerUrl { get; init; } = default!;
    public string Token      { get; init; } = "";
}
