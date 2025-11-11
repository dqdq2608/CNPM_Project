using Duende.Bff.Yarp;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;

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

app.MapBffPublicApi();
app.MapBffManagementEndpoints();

app.Run();