using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;

namespace IdentityServerBFF;

// DTOs
public sealed record LoginRequest(string username, string password);
public sealed record BffEntry(string type, string value, string? valueType);
public sealed record BffUserDto(string? sub, string? name, string? email, int session_expires_in, IEnumerable<BffEntry> raw);

public static class BffPublicApi
{
    /// Gom toàn bộ API FE cần vào 1 nhóm `/bff/public/*`
    /// - GET  /bff/public/antiforgery
    /// - POST /bff/public/login
    /// - GET  /bff/public/user
    /// - POST /bff/public/logout
    public static IEndpointRouteBuilder MapBffPublicApi(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/bff/public");

        // 1) CSRF handshake
        g.MapGet("/antiforgery", (IAntiforgery anti, HttpContext ctx) =>
        {
            var tokens = anti.GetAndStoreTokens(ctx);
            ctx.Response.Cookies.Append(
                "__Host-bff-csrf",
                tokens.RequestToken!,
                new CookieOptions { HttpOnly = false, Secure = true, SameSite = SameSiteMode.None, Path = "/" }
            );
            return Results.Ok(new { ok = true });
        })
        .AllowAnonymous()
        .DisableAntiforgery();

        // 2) Login (ROPC -> tạo cookie __Host-bff)
        g.MapPost("/login", LoginHandler)
         .AllowAnonymous()
         .DisableAntiforgery();

        // 3) Đọc phiên hiện tại
        g.MapGet("/user", async (HttpContext ctx) =>
        {
            var auth = await ctx.AuthenticateAsync("cookie");
            if (!auth.Succeeded || auth.Principal is null) return Results.Unauthorized();

            var claims = auth.Principal.Claims.Select(c => new BffEntry(c.Type, c.Value, null)).ToList();
            var sub   = claims.FirstOrDefault(c => c.type == "sub")?.value;
            var name  = claims.FirstOrDefault(c => c.type == "name")?.value;
            var email = claims.FirstOrDefault(c => c.type == "email")?.value;

            var expiresUtc = auth.Properties?.ExpiresUtc;
            var expiresIn  = expiresUtc.HasValue ? Math.Max(0, (int)(expiresUtc.Value - DateTimeOffset.UtcNow).TotalSeconds) : 0;

            claims.Add(new("bff:logout_url", "/bff/public/logout", null));
            claims.Add(new("bff:session_expires_in", expiresIn.ToString(), null));

            return Results.Ok(new BffUserDto(sub, name, email, expiresIn, claims));
        })
        .RequireAuthorization()
        .DisableAntiforgery();

        // 4) Logout (yêu cầu CSRF)
        g.MapPost("/logout", async (HttpContext http) =>
        {
            try
            {
                await http.SignOutAsync("cookie");
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                http.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("BFF")
                    .LogError(ex, "Logout failed");
                return Results.Problem("Logout failed", statusCode: 400);
            }
        })
        .RequireAuthorization()
        .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> LoginHandler(
        HttpContext http, LoginRequest dto, IHttpClientFactory httpFactory, IConfiguration cfg)
    {
        var bff = cfg.GetSection("BFF").Get<Configuration>()!;
        var ids = httpFactory.CreateClient("ids");
        var tokenEndpoint = $"{bff.Authority!.TrimEnd('/')}/connect/token";

        var scopes = string.Join(' ', bff.Scopes ?? new List<string>());

        var form = new Dictionary<string, string>
        {
            ["grant_type"]    = "password",
            ["client_id"]     = bff.ClientId!,
            ["client_secret"] = bff.ClientSecret!,
            ["username"]      = dto.username,
            ["password"]      = dto.password,
            ["scope"]         = scopes
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        { Content = new FormUrlEncodedContent(form) };

        var resp = await ids.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            http.Response.StatusCode = (int)resp.StatusCode;
            return Results.Content(body, "application/json");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var accessToken  = root.GetProperty("access_token").GetString()!;
        var refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var expiresIn    = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600;

        // 1) Đọc claims cơ bản từ access_token
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var claims = jwt.Claims
                        .Where(c => c.Type is "sub" or "name" or "email" or "role")
                        .Select(c => new Claim(c.Type, c.Value))
                        .ToList();

        // Helper: thêm claim nếu chưa có cặp (type,value)
        static void AddClaimIfNotExists(List<Claim> list, string type, string value)
        {
            if (!list.Any(c => c.Type == type && c.Value == value))
                list.Add(new Claim(type, value));
        }

        // 2) GỌI USERINFO để lấy profile đầy đủ rồi MERGE (xử lý mọi kiểu JSON)
        var authority = bff.Authority!.TrimEnd('/');
        var userInfoEndpoint = $"{authority}/connect/userinfo";
        using (var uiReq = new HttpRequestMessage(HttpMethod.Get, userInfoEndpoint))
        {
            uiReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var uiResp = await ids.SendAsync(uiReq);
            if (uiResp.IsSuccessStatusCode)
            {
                var uiBody = await uiResp.Content.ReadAsStringAsync();
                using var uiDoc = JsonDocument.Parse(uiBody);
                foreach (var prop in uiDoc.RootElement.EnumerateObject())
                {
                    var t = prop.Name;
                    var v = prop.Value;

                    switch (v.ValueKind)
                    {
                        case JsonValueKind.String:
                            AddClaimIfNotExists(claims, t, v.GetString()!);
                            break;
                        case JsonValueKind.Number:
                            AddClaimIfNotExists(claims, t, v.GetRawText()); // giữ nguyên dạng chuỗi số
                            break;
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            AddClaimIfNotExists(claims, t, v.GetBoolean().ToString().ToLowerInvariant());
                            break;
                        case JsonValueKind.Array:
                            foreach (var item in v.EnumerateArray())
                            {
                                if (item.ValueKind == JsonValueKind.String)
                                    AddClaimIfNotExists(claims, t, item.GetString()!);
                                else
                                    AddClaimIfNotExists(claims, t, item.GetRawText());
                            }
                            break;
                        case JsonValueKind.Null:
                        case JsonValueKind.Object:
                        case JsonValueKind.Undefined:
                        default:
                            break;
                    }
                }
            }
        }

        // 3) Đảm bảo có "sub"
        if (!claims.Any(c => c.Type == "sub"))
            claims.Add(new Claim("sub", Guid.NewGuid().ToString("N")));

        // 4) Lưu session cookie
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
    }

    private sealed record Configuration
    {
        public string? Authority { get; init; }
        public string? ClientId { get; init; }
        public string? ClientSecret { get; init; }
        public List<string>? Scopes { get; init; }
    }
}
