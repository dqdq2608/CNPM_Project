using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

using Duende.Bff;
using Duende.Bff.Yarp;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http.Metadata;

using IdentityServerBFF;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------
// BFF & cấu hình chung
// -----------------------------
builder.Services.AddBff()
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
builder.Services.AddHttpClient("ids");

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

// GET /bff/antiforgery (sinh token)
app.MapGet("/bff/antiforgery", (IAntiforgery anti, HttpContext ctx) =>
{
    var tokens = anti.GetAndStoreTokens(ctx); // __Host-bff-af (HttpOnly)

    // Trả thêm cookie FE-đọc-được để gắn vào header X-CSRF
    ctx.Response.Cookies.Append(
        "__Host-bff-csrf",
        tokens.RequestToken!,
        new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/"
        });

    return Results.Ok(new { ok = true });
})
.AllowAnonymous()
.DisableAntiforgery();

// cho FE biết phiên đã có và còn bao lâu
app.MapGet("/bff/user", async (HttpContext ctx) =>
{
    var auth = await ctx.AuthenticateAsync("cookie");
    if (!auth.Succeeded || auth.Principal == null)
        return Results.Unauthorized();

    var list = auth.Principal.Claims
        .Select(c => new BffEntry(c.Type, c.Value, null))
        .ToList();

    var expiresUtc = auth.Properties?.ExpiresUtc;
    var expiresIn = expiresUtc.HasValue
        ? Math.Max(0, (int)(expiresUtc.Value - DateTimeOffset.UtcNow).TotalSeconds)
        : 0;

    list.Add(new BffEntry("bff:logout_url", "/auth/logout", null));
    list.Add(new BffEntry("bff:session_expires_in", expiresIn.ToString(), null));

    return Results.Json(list);
})
.RequireAuthorization()
.DisableAntiforgery();

// POST /auth/password-login
app.MapPost("/auth/password-login", async (HttpContext http, LoginDto dto, IHttpClientFactory httpFactory, IConfiguration cfg) =>
{
    // Gọi IDS để trao đổi usr/psswd và access_token
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
    { Content = new FormUrlEncodedContent(form) };

    var resp = await ids.SendAsync(req);
    var body = await resp.Content.ReadAsStringAsync();

    Console.WriteLine($"[TOKEN RESP] {resp.StatusCode}: {body}");

    if (!resp.IsSuccessStatusCode)
    {
        http.Response.StatusCode = (int)resp.StatusCode;
        return Results.Content(body, "application/json");
    }

    // Lấy access_token & đọc claims cơ bản
    using var doc = JsonDocument.Parse(body);
    var root = doc.RootElement;

    var accessToken  = root.GetProperty("access_token").GetString()!;
    var refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
    var expiresIn    = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600;

    var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

    var claims = jwt.Claims
        .Where(c => c.Type is "sub" or "name" or "email" or "role")
        .ToList();

    if (!claims.Any(c => c.Type == "sub"))
        claims.Add(new Claim("sub", Guid.NewGuid().ToString("N")));

    // Tạo session cookie __Host-bff (HttpOnly)
    var identity = new ClaimsIdentity(claims, "cookie", "name", "role");
    var user = new ClaimsPrincipal(identity);

    var props = new AuthenticationProperties
    {
        IsPersistent = false,
        ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60)
    };
    props.StoreTokens(new[]
    {
        new AuthenticationToken { Name = "access_token",  Value = accessToken },
        new AuthenticationToken { Name = "token_type",    Value = "Bearer" },
        new AuthenticationToken { Name = "expires_at",    Value = DateTime.UtcNow.AddSeconds(expiresIn - 60).ToString("o") },
        new AuthenticationToken { Name = "refresh_token", Value = refreshToken ?? "" }
    });

    await http.SignInAsync("cookie", user, props);
    return Results.Ok(new { ok = true });
})
.AllowAnonymous()
.DisableAntiforgery();

// POST /auth/logout
app.MapPost("/auth/logout", async (HttpContext http) =>
{
    try
    {
        await http.SignOutAsync("cookie");
    }
    catch { /* nuốt lỗi dev */ }

    return Results.Ok(new { ok = true });
})
.RequireAuthorization()
.DisableAntiforgery(); // <-- quan trọng trong dev để tránh 500

app.Run();

// Models
public sealed record LoginDto(string username, string password);
public sealed record BffEntry(string type, string value, string? valueType);
