using Duende.Bff.Yarp;
using System.Security.Claims;
using Duende.Bff;
using System.Text.Json;
using System.Text;
using System.Linq;
using System.Net.Http;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net.Security;

using IdentityServerBFF;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------
// BFF & cấu hình chung
// -----------------------------
builder.Services.AddBff(o =>
    {
        // tắt cơ chế tự revoke refresh token khi sign-out
        o.RevokeRefreshTokenOnLogout = false;
    })
    .AddServerSideSessions()
    .AddRemoteApis();

// Bind config BFF (Authority, ClientId, ...)
var bffConfig = new Configuration();
builder.Configuration.Bind("BFF", bffConfig);

// CORS cho FE https://localhost:3000
builder.Services.AddCors(o => o.AddPolicy("fe", p => p
    .WithOrigins("https://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
));

builder.Services.AddAuthentication(o =>
{
    o.DefaultScheme = "cookie";
    o.DefaultChallengeScheme = "cookie";
    o.DefaultSignOutScheme = "cookie";
})
.AddCookie("cookie", o =>
{
    o.Cookie.Name = "__Host-bff";
    o.Cookie.Path = "/";
    o.Cookie.SameSite = SameSiteMode.None;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.SlidingExpiration = true;

    o.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; },
        OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "X-CSRF";
    o.Cookie.Name = "__Host-bff-af";
    o.Cookie.Path = "/";
    o.Cookie.SameSite = SameSiteMode.None;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// HttpClient gọi IdentityServer
builder.Services.AddHttpClient("ids", (sp, c) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();

    // luôn có default => không còn nullable warning
    var authority = cfg["BFF:Authority"]
                    ?? bffConfig.Authority
                    ?? "https://localhost:5001";

    c.BaseAddress = new Uri(authority.TrimEnd('/'));
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// gọi Kong
var kongUrl = builder.Configuration["Kong:Url"] ?? "https://localhost:8443";
var kongApiKey = builder.Configuration["Kong:ApiKey"] ?? "bff-internal-api-key";

builder.Services.AddHttpClient("kong", c =>
{
    c.BaseAddress = new Uri(kongUrl);
    c.DefaultRequestHeaders.Add("apikey", kongApiKey);
    c.Timeout = TimeSpan.FromSeconds(5);
})
// chấp nhận self-signed cert chỉ cho môi trường DEV (không ảnh hưởng prod)
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// HttpClient để gọi Basket
builder.Services.AddHttpClient("basket", (sp, c) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["Basket:BaseUrl"] ?? "http://localhost:5005";
    c.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});

app.UseHttpsRedirection();
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.None,
    Secure = CookieSecurePolicy.Always
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("fe");

app.UseAuthentication();

app.UseBff();
app.UseAuthorization();

app.MapBffPublicApi();
app.MapBffManagementEndpoints();

app.MapGet("/debug/user", (ClaimsPrincipal user) =>
{
    var isAuth = user?.Identity?.IsAuthenticated ?? false;

    return Results.Json(new
    {
        isAuth,
        name = user.Identity?.Name,
        claims = user.Claims.Select(c => new { c.Type, c.Value })
    });
});


app.MapGet("/api/health", () =>
{
    return Results.Json(new { status = "ok", service = "BFF", time = DateTime.UtcNow });
});

app.MapGet("/basket/test", () =>
{
    return Results.Json(new { ok = true, from = "BFF", path = "/basket/test" });
});

app.MapGet("/api/kong-check", async (IHttpClientFactory f) =>
{
    var http = f.CreateClient("kong");
    var res = await http.GetAsync("/internal/ping");
    var body = await res.Content.ReadAsStringAsync();
    return Results.Content(body, "application/json");
});

app.MapGet("/api/catalog/items", async (IHttpClientFactory f) =>
{
    var http = f.CreateClient("kong");
    var res = await http.GetAsync("/catalog/api/catalog/items");
    var json = await res.Content.ReadAsStringAsync();
    return Results.Content(json, "application/json");
});


// GET /bff-api/basket
app.MapGet("/bff-api/basket", async (HttpContext ctx, IHttpClientFactory f) =>
{
    // 1. Kiểm tra đã login chưa
    var user = ctx.User;
    if (user?.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    // 2. Lấy buyerId từ claim "sub"
    var sub = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(sub))
    {
        return Results.Unauthorized();
    }

    // 3. Gọi Basket.API qua HttpClient "basket"
    var client = f.CreateClient("basket");

    // Gọi endpoint: GET /api/basket  (BasketApi sẽ tự lấy buyerId từ header)
    var req = new HttpRequestMessage(HttpMethod.Get, "/api/basket");
    req.Headers.Add("X-User-Sub", sub);

    var res = await client.SendAsync(req);
    var body = await res.Content.ReadAsStringAsync();

    if (res.IsSuccessStatusCode)
    {
        return Results.Text(body, "application/json", Encoding.UTF8);
    }

    return Results.StatusCode((int)res.StatusCode);
})
.RequireAuthorization(); // có thể thêm .AsBffApiEndpoint() nếu muốn bảo vệ CSRF chặt hơn

// POST /bff-api/basket
app.MapPost("/bff-api/basket", async (HttpContext ctx, IHttpClientFactory f) =>
{
    var user = ctx.User;
    var sub = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(sub))
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    using var bodyStream = new StreamReader(ctx.Request.Body, Encoding.UTF8);
    var json = await bodyStream.ReadToEndAsync();
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    using var ms = new MemoryStream();
    using (var writer = new Utf8JsonWriter(ms))
    {
        writer.WriteStartObject();

        foreach (var prop in root.EnumerateObject())
        {
            if (prop.NameEquals("buyerId") || prop.NameEquals("BuyerId"))
                continue;

            prop.WriteTo(writer);
        }

        writer.WriteString("buyerId", sub);
        writer.WriteEndObject();
    }

    ms.Position = 0;

    var http = f.CreateClient("basket");
    using var content = new StreamContent(ms);
    content.Headers.ContentType =
        new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

    var res = await http.PostAsync("/api/basket", content);

    ctx.Response.StatusCode = (int)res.StatusCode;
    ctx.Response.ContentType = "application/json";
    await res.Content.CopyToAsync(ctx.Response.Body);
})
.RequireAuthorization(); // .AsBffApiEndpoint() nếu muốn

// DELETE /bff-api/basket
app.MapDelete("/bff-api/basket", async (HttpContext ctx, IHttpClientFactory f) =>
{
    var user = ctx.User;
    var sub = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(sub))
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    var http = f.CreateClient("basket");
    // xóa bằng API "delete giỏ của user hiện tại"
    var res = await http.DeleteAsync("/api/basket");

    ctx.Response.StatusCode = (int)res.StatusCode;
})
.RequireAuthorization(); // .AsBffApiEndpoint()

app.Run();