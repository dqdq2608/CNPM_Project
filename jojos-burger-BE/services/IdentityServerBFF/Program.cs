using IdentityServerBFF;
using Duende.Bff.Yarp;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBff()
    .AddRemoteApis();

Configuration config = new();
builder.Configuration.Bind("BFF", config);

// Add CORS cho FE (khác origin)
builder.Services.AddCors(o => o.AddPolicy("fe", p => p
    .WithOrigins("http://localhost:3000")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()
));

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "cookie";
        options.DefaultChallengeScheme = "oidc";
        options.DefaultSignOutScheme = "oidc";
    })
    .AddCookie("cookie", options =>
    {
        options.Cookie.Name = "__Host-bff";
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    })
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = config.Authority;
        options.ClientId = config.ClientId;
        options.ClientSecret = config.ClientSecret;

        options.ResponseType = "code";
        options.ResponseMode = "query";

        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;
        options.SaveTokens = true;

        options.Scope.Clear();
        foreach (var scope in config.Scopes)
        {
            options.Scope.Add(scope);
        }

        // options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new()
        {
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Nếu đứng sau reverse proxy/nginx hoặc chạy nhiều scheme/host giúp đảm bảo redirect/cookie đúng
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

// Sủ dụng CORS cho FE đã khai báo ở trên
app.UseCors("fe");

// 👇 Endpoint sống
app.MapGet("/health", () =>
    Results.Json(new { ok = true, service = "bff", ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() })
);

// (tuỳ chọn) stub login để FE test
app.MapPost("/auth/password-login", () =>
    Results.Json(new { access_token = "fake-token", token_type = "Bearer", expires_in = 3600, role = "user" })
);

app.UseAuthentication();
app.UseBff();
app.UseAuthorization();

app.MapBffManagementEndpoints();

if (config.Apis.Any())
{
    foreach (var api in config.Apis)
    {
        app.MapRemoteBffApiEndpoint(api.LocalPath, api.RemoteUrl!)
            .RequireAccessToken(api.RequiredToken);
    }
}

app.Run();
