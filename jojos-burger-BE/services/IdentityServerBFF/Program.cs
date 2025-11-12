using Duende.Bff.Yarp;

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
    var authority = cfg["BFF:Authority"] ?? bffConfig.Authority;
    c.BaseAddress = new Uri(authority.TrimEnd('/'));
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// gọi Kong
var kongUrl   = builder.Configuration["Kong:Url"]   ?? "https://localhost:8443";
var kongApiKey= builder.Configuration["Kong:ApiKey"]?? "bff-internal-api-key";

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

app.MapGet("/api/health", () =>
{
    return Results.Json(new { status = "ok", service = "BFF", time = DateTime.UtcNow });
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
    var res  = await http.GetAsync("/catalog/api/catalog/items");
    var json = await res.Content.ReadAsStringAsync();
    return Results.Content(json, "application/json");
});

app.Run();