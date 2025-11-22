using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Duende.IdentityModel.Client;
using IdentityServerBFF.Application.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;

namespace IdentityServerBFF;

// ================= DTOs =================

public sealed record LoginRequest(string username, string password);

public sealed record BffEntry(string type, string value, string? valueType);

public sealed record BffUserDto(
    string? sub,
    string? name,
    string? email,
    int session_expires_in,
    IEnumerable<BffEntry> raw);

// ================= BFF PUBLIC API =================

public static class BffAuthApi
{
    public static IEndpointRouteBuilder MapBffAuthApi(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/bff/public");

        MapAntiforgery(g);
        MapLogin(g);
        MapUser(g);
        MapLogout(g);

        return app;
    }

    // 1) CSRF handshake: FE phải gọi trước khi POST login/logout
    private static void MapAntiforgery(IEndpointRouteBuilder g)
    {
        g.MapGet("/antiforgery", (IAntiforgery anti, HttpContext ctx) =>
        {
            var tokens = anti.GetAndStoreTokens(ctx);

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
    }

    // 2) Login (ROPC -> gọi IDS, tạo cookie session cho BFF)
    private static void MapLogin(IEndpointRouteBuilder g)
    {
        g.MapPost("/login", LoginHandler)
         .AllowAnonymous()
         .DisableAntiforgery();
    }

    // 3) Đọc phiên hiện tại từ cookie
    private static void MapUser(IEndpointRouteBuilder g)
    {
        g.MapGet("/user", async (HttpContext ctx) =>
        {
            var auth = await ctx.AuthenticateAsync("cookie");
            if (!auth.Succeeded || auth.Principal is null)
                return Results.Unauthorized();

            var claims = auth.Principal.Claims
                .Select(c => new BffEntry(c.Type, c.Value, null))
                .ToList();

            string? Get(string type) =>
                claims.FirstOrDefault(c => c.type == type)?.value;

            var sub   = Get("sub");
            var name  = Get("name");
            var email = Get("email");

            var expiresUtc = auth.Properties?.ExpiresUtc;
            var expiresIn  = expiresUtc.HasValue
                ? Math.Max(0, (int)(expiresUtc.Value - DateTimeOffset.UtcNow).TotalSeconds)
                : 0;

            claims.Add(new("bff:logout_url", "/bff/public/logout", null));
            claims.Add(new("bff:session_expires_in", expiresIn.ToString(), null));

            return Results.Ok(new BffUserDto(sub, name, email, expiresIn, claims));
        })
        .RequireAuthorization()
        .DisableAntiforgery();
    }

    // 4) Logout: xoá cookie session
    private static void MapLogout(IEndpointRouteBuilder g)
    {
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
    }

    // ================= LOGIN HANDLER =================

    /// <summary>
    /// - Gọi IDS bằng ROPC (/connect/token)
    /// - Gọi /connect/userinfo để lấy thêm claim
    /// - Gộp claim lại -> tạo cookie "cookie"
    /// </summary>
    private static async Task<IResult> LoginHandler(
        HttpContext http,
        LoginRequest dto,
        IBffBackchannelToIds backchannel,
        IConfiguration cfg,
        CancellationToken ct)
    {
        var bff = cfg.GetSection("BFF").Get<Configuration>()!;
        var scopes = string.Join(' ', bff.Scopes ?? new List<string>());

        // 1) Gọi IDS lấy token bằng ROPC qua backchannel
        TokenResponse tokenResponse;
        try
        {
            tokenResponse = await backchannel.PasswordAsync(
                dto.username,
                dto.password,
                scopes,
                ct);
        }
        catch (Exception ex)
        {
            // Token error: trả nguyên thông tin lỗi về FE
            http.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("BFF")
                .LogError(ex, "Login failed");

            return Results.Problem("Login failed", statusCode: 400);
        }

        var accessToken  = tokenResponse.AccessToken!;
        var refreshToken = tokenResponse.RefreshToken;
        var expiresIn    = tokenResponse.ExpiresIn > 0 ? tokenResponse.ExpiresIn : 3600;

        // 2) Lấy claim cơ bản từ access_token (JWT)
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var claims = jwt.Claims
                        .Where(c => c.Type is "sub" or "name" or "email" or "role")
                        .Select(c => new Claim(c.Type, c.Value))
                        .ToList();

        static void AddClaimIfNotExists(List<Claim> list, string type, string value)
        {
            if (!list.Any(c => c.Type == type && c.Value == value))
                list.Add(new Claim(type, value));
        }

        // 3) Gọi /connect/userinfo qua backchannel để lấy thêm claim
        try
        {
            var userInfo = await backchannel.GetUserInfoAsync(accessToken, ct);

            foreach (var ci in userInfo.Claims)
            {
                AddClaimIfNotExists(claims, ci.Type, ci.Value);
            }
        }
        catch (Exception ex)
        {
            // lỗi userinfo không phải critical -> log rồi bỏ qua
            http.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("BFF")
                .LogWarning(ex, "GetUserInfo failed");
        }

        // 4) Đảm bảo có "sub"
        if (!claims.Any(c => c.Type == "sub"))
            claims.Add(new Claim("sub", Guid.NewGuid().ToString("N")));

        // 5) Lưu session vào cookie "cookie"
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
