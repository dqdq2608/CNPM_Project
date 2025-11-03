using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Duende.Bff;
using Duende.Bff.Yarp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using IdentityServerBFF;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// BFF + server-side sessions
builder.Services.AddBff()
    .AddServerSideSessions()
    .AddRemoteApis();

// bind cấu hình BFF từ appsettings
var bffConfig = new Configuration();
builder.Configuration.Bind("BFF", bffConfig);

// CORS cho FE (HTTPS 3000)
builder.Services.AddCors(o => o.AddPolicy("fe", p => p
    .WithOrigins("https://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
));

// Cookie auth (không OIDC redirect)
builder.Services.AddAuthentication(o =>
{
    o.DefaultScheme = "cookie";
    o.DefaultChallengeScheme = "cookie";
    o.DefaultSignOutScheme = "cookie";
})
.AddCookie("cookie", o =>
{
    o.Cookie.Name = "__Host-bff";
    o.Cookie.SameSite = SameSiteMode.None;      // khác origin -> bắt buộc None + HTTPS
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.SlidingExpiration = true;

    // tránh redirect 302 khi gọi API
    o.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; },
        OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; }
    };
});

builder.Services.AddAuthorization();

// HttpClient gọi IdentityServer
builder.Services.AddHttpClient("ids");

var app = builder.Build();

// nếu đứng sau reverse proxy
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("fe");

app.UseAuthentication();
app.UseBff();
app.UseAuthorization();

// /bff/user, /bff/backchannel, ...
app.MapBffManagementEndpoints();

app.MapGet("/bff/antiforgery", (HttpContext ctx) =>
{
    var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    ctx.Response.Cookies.Append("BffCsrf", token, new CookieOptions
    {
        HttpOnly = false,            // FE đọc được
        Secure = true,               // bắt buộc HTTPS
        SameSite = SameSiteMode.None,
        IsEssential = true,
        Path = "/"
    });
    return Results.Ok(new { ok = true });
});

app.MapGet("/debug/echo", (HttpContext ctx) =>
{
    var cookies = string.Join("; ", ctx.Request.Cookies.Select(kv => $"{kv.Key}={kv.Value}"));
    return Results.Text($"Cookie: {cookies}\nX-CSRF: {ctx.Request.Headers["X-CSRF"]}");
});

// ===== Login (ROPC) =====
// POST https://bff/auth/password-login  body: { username, password }
app.MapPost("/auth/password-login", async (HttpContext http, LoginDto dto, IHttpClientFactory httpFactory, IConfiguration cfg) =>
{
    var cfgBff = cfg.GetSection("BFF").Get<Configuration>()!;
    var ids = httpFactory.CreateClient("ids");
    var tokenEndpoint = $"{cfgBff.Authority!.TrimEnd('/')}/connect/token";

    var scopes = (cfgBff.Scopes is { Count: > 0 })
        ? string.Join(' ', cfgBff.Scopes)
        : "openid profile offline_access api";

    var form = new Dictionary<string, string>
    {
        ["grant_type"]    = "password",
        ["client_id"]     = cfgBff.ClientId!,
        ["client_secret"] = cfgBff.ClientSecret!,
        ["username"]      = dto.username,
        ["password"]      = dto.password,
        ["scope"]         = scopes
    };

    using var req = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
    {
        Content = new FormUrlEncodedContent(form)
    };

    var resp = await ids.SendAsync(req);
    var body = await resp.Content.ReadAsStringAsync();

    Console.WriteLine($"[TOKEN RESP] {resp.StatusCode}: {body}");

    if (!resp.IsSuccessStatusCode)
    {
        // trả nguyên body lỗi về FE (để debug), status code cũng set đúng
        http.Response.StatusCode = (int)resp.StatusCode;
        return Results.Content(body, "application/json");
    }

    // parse token json
    using var doc = JsonDocument.Parse(body);
    var root = doc.RootElement;
    var accessToken  = root.GetProperty("access_token").GetString()!;
    var refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
    var expiresIn    = root.TryGetProperty("expires_in",   out var ei) ? ei.GetInt32() : 3600;

    // đọc claim từ access_token
    var handler = new JwtSecurityTokenHandler();
    var jwt = handler.ReadJwtToken(accessToken);

    var claims = new List<Claim>();
    foreach (var c in jwt.Claims)
    {
        if (c.Type is "sub" or "name" or "email" or "role") claims.Add(c);
    }
    if (!claims.Any(c => c.Type == "sub"))
        claims.Add(new Claim("sub", Guid.NewGuid().ToString("N")));

    var identity = new ClaimsIdentity(claims, "cookie", "name", "role");
    var user = new ClaimsPrincipal(identity);

    // lưu token vào auth properties (để BFF có thể dùng refresh sau này nếu cần)
    var props = new AuthenticationProperties
    {
        IsPersistent = false,
        ExpiresUtc   = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60)
    };
    props.StoreTokens(new[]
    {
        new AuthenticationToken { Name = "access_token",  Value = accessToken },
        new AuthenticationToken { Name = "token_type",    Value = "Bearer" },
        new AuthenticationToken { Name = "expires_at",    Value = DateTime.UtcNow.AddSeconds(expiresIn - 60).ToString("o") },
        new AuthenticationToken { Name = "refresh_token", Value = refreshToken ?? "" }
    });

    await http.SignInAsync("cookie", user, props);

    return Results.Json(new { ok = true }); // 200
})
.AllowAnonymous()
.DisableAntiforgery(); // login không cần CSRF

// health
app.MapGet("/health", () => Results.Ok(new { ok = true }));

// logout (có CSRF)
app.MapPost("/auth/logout", async (HttpContext http) =>
{
    await http.SignOutAsync("cookie");
    return Results.Ok(new { ok = true });
})
.RequireAuthorization()
.AsBffApiEndpoint();

// map các remote API (nếu có)
if (bffConfig.Apis?.Any() == true)
{
    foreach (var api in bffConfig.Apis)
    {
        app.MapRemoteBffApiEndpoint(api.LocalPath, api.RemoteUrl!)
           .RequireAccessToken(api.RequiredToken);
    }
}

app.Run();

// DTO
public sealed record LoginDto(string username, string password);
