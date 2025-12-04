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

using IdentityServerBFF.Application.Identity;
using IdentityServerBFF.Infrastructure.Identity;
using IdentityServerBFF.Application.Services;
using IdentityServerBFF.Infrastructure.Services;


using IdentityServerBFF;

var builder = WebApplication.CreateBuilder(args);

// BFF & cấu hình chung
builder.Services.AddBff(o =>
    {
        // tắt cơ chế tự revoke refresh token khi sign-out
        o.RevokeRefreshTokenOnLogout = false;
    })
    .AddServerSideSessions()
    .AddRemoteApis();

// DI cho Interface IBffBackchannelToIds
builder.Services.AddHttpClient<IBffBackchannelToIds, BffBackchannelToIds>();

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

// Đăng kí ICatalogBffApi
builder.Services.AddHttpClient<ICatalogBffApi, CatalogBffApi>(c =>
{
    c.BaseAddress = new Uri(kongUrl);
    c.DefaultRequestHeaders.Add("apikey", kongApiKey);
    c.Timeout = TimeSpan.FromSeconds(5);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// PROD: Gọi Payment service thật qua Kong
builder.Services.AddHttpClient<IPaymentApi, CheckoutOnlineBffApi>(c =>
{
    c.BaseAddress = new Uri(kongUrl);
    c.DefaultRequestHeaders.Add("apikey", kongApiKey);
    c.Timeout = TimeSpan.FromSeconds(5);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// HttpClient để gọi Basket
builder.Services.AddHttpClient("basket", (sp, c) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["Basket:BaseUrl"] ?? "http://localhost:5005";
    c.BaseAddress = new Uri(baseUrl);
});

// HttpClient để gọi Ordering
builder.Services.AddHttpClient("ordering", (sp, c) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["Ordering:BaseUrl"] ?? "http://ordering-api";
    c.BaseAddress = new Uri(baseUrl);
}).AddUserAccessTokenHandler();

// HttpClient để gọi Delivery
builder.Services.AddHttpClient("delivery", (sp, c) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["Delivery:BaseUrl"] ?? "http://delivery-api";
    c.BaseAddress = new Uri(baseUrl);
});

// Đăng kí IOrderBffApi (Order có dùng nhiều HttpClient, nên dùng Scoped + IHttpClientFactory)
builder.Services.AddScoped<IOrderBffApi, OrderBffApi>();

// Đăng kí IGeocodingService (Fake)
builder.Services.AddSingleton<IGeocodingService, FakeGeocodingService>();

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

app.MapBffAuthApi();
app.MapBffManagementEndpoints();
app.MapBffPublicApi();

app.MapGet("/debug/user", (ClaimsPrincipal? user) =>
{
    var isAuth = user?.Identity?.IsAuthenticated ?? false;

    return Results.Json(new
    {
        isAuth,
        name = user?.Identity?.Name,
        claims = user?.Claims
            .Select(c => new { c.Type, c.Value })
            ?? Enumerable.Empty<object>()
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

    // ✅ Tạo HttpRequestMessage để gắn header X-User-Sub
    var req = new HttpRequestMessage(HttpMethod.Post, "/api/basket");
    req.Headers.Add("X-User-Sub", sub);
    req.Content = content;

    var res = await http.SendAsync(req);

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

    // Tạo request DELETE /api/basket và forward buyerId qua header
    var req = new HttpRequestMessage(HttpMethod.Delete, "/api/basket");
    req.Headers.Add("X-User-Sub", sub);

    var res = await http.SendAsync(req);

    ctx.Response.StatusCode = (int)res.StatusCode;
})
.RequireAuthorization(); // .AsBffApiEndpoint()


app.MapPost("/bff-api/order", async (
    ClaimsPrincipal user,
    FrontCreateOrderRequest request,
    IOrderBffApi orderBffApi,
    ILogger<Program> logger) =>
{
    try
    {
        var resultJson = await orderBffApi.CreateOrderFromBasketAsync(user, request);
        return Results.Content(resultJson, "application/json");
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Business error when creating order");
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error when creating order");
        // tạm trả detail để debug
        return Results.Problem(
            title: "Unexpected error when creating order",
            detail: ex.ToString(),
            statusCode: 500
        );
    }
})
.RequireAuthorization();


app.MapGet("/bff-api/orders", async (
    ClaimsPrincipal user,
    IOrderBffApi orderBffApi) =>
{
    try
    {
        var json = await orderBffApi.GetOrdersForUserAsync(user);
        return Results.Content(json, "application/json");
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        // log nếu muốn
        Console.Error.WriteLine(ex);
        return Results.StatusCode(500);
    }
})
.RequireAuthorization();

app.MapGet("/bff-api/restaurant/orders",
    async (ClaimsPrincipal user, IOrderBffApi api) =>
    {
        var json = await api.GetRestaurantOrdersAsync(user);
        return Results.Content(json, "application/json");
    })
   .RequireAuthorization();


app.MapPost("/bff-api/restaurant/orders/{orderId:int}/start-delivery",
    async (int orderId, StartDeliveryRequest request, IOrderBffApi api) =>
    {
        await api.StartDeliveryAsync(orderId, request.DroneId);
        return Results.Ok(new { success = true });
    })
   .RequireAuthorization();


app.MapPost("/bff-api/orders/{orderId:int}/delivery/tick",
    async (ClaimsPrincipal user, int orderId, IOrderBffApi api) =>
    {
        var json = await api.TickDeliveryAsync(user, orderId);
        return Results.Content(json, "application/json");
    })
    .RequireAuthorization();

app.MapPost("/bff-api/orders/{orderId:int}/confirm-delivery",
    async (ClaimsPrincipal user, int orderId, IOrderBffApi OrderBffApi) =>
    {
        await OrderBffApi.ConfirmDeliveryAsync(user, orderId);
        return Results.Ok(new { success = true });
    })
    .RequireAuthorization();

app.MapGet("/bff-api/orders/{orderId:int}", async (
    int orderId,
    ClaimsPrincipal user,
    IOrderBffApi orderBffApi) =>
{
    try
    {
        var json = await orderBffApi.GetOrderDetailAsync(user, orderId);
        return Results.Content(json, "application/json");
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
    catch
    {
        return Results.StatusCode(500);
    }
})
.RequireAuthorization();

app.MapGet("/bff-api/orders/{orderId:int}/delivery",
    async (ClaimsPrincipal user, int orderId, IOrderBffApi api) =>
    {
        var json = await api.GetDeliveryForOrderAsync(user, orderId);
        return Results.Content(json, "application/json");
    })
    .RequireAuthorization();

app.MapPost("/bff-api/delivery/quote", async (
    ClaimsPrincipal user,
    DeliveryQuoteRequest request,
    IOrderBffApi orderBffApi) =>
{
    try
    {
        var quote = await orderBffApi.GetDeliveryQuoteAsync(user, request);
        return Results.Json(quote);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex);
        return Results.StatusCode(500);
    }
})
.RequireAuthorization();

// ====== BFF API cho Drone Management ======

// ====== BFF API cho Drone Management ======

// POST /bff-api/drones/{id}/tick
app.MapPost("/bff-api/drones/{id:int}/tick",
    async (int id, IHttpClientFactory f) =>
    {
        var http = f.CreateClient("delivery");
        var res = await http.PostAsync($"/api/drones/{id}/tick", null);
        return Results.StatusCode((int)res.StatusCode);
    })
    .RequireAuthorization();

// GET /bff-api/drones
app.MapGet("/bff-api/drones", async (HttpContext ctx, IHttpClientFactory f) =>
{
    var user = ctx.User;
    if (user?.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    // Lấy restaurantId từ claim (Guid)
    var restaurantIdStr = user.FindFirst("restaurant_id")?.Value;
    if (string.IsNullOrWhiteSpace(restaurantIdStr) || !Guid.TryParse(restaurantIdStr, out var restaurantId))
    {
        return Results.BadRequest(new { message = "Missing or invalid restaurant_id claim" });
    }

    var http = f.CreateClient("delivery");

    // Gọi Delivery.API: GET /api/drones?restaurantId=<guid>
    var res = await http.GetAsync($"/api/drones?restaurantId={restaurantId}");
    var body = await res.Content.ReadAsStringAsync();

    if (res.IsSuccessStatusCode)
    {
        return Results.Text(body, "application/json", Encoding.UTF8);
    }

    return Results.StatusCode((int)res.StatusCode);
})
.RequireAuthorization();

// POST /bff-api/drones  (tạo drone mới cho restaurant hiện tại)
app.MapPost("/bff-api/drones", async (
    HttpContext ctx,
    IHttpClientFactory f
) =>
{
    var user = ctx.User;
    if (user?.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    // 1. Lấy restaurantId (Guid) từ claim
    var restaurantIdStr = user.FindFirst("restaurant_id")?.Value;
    if (string.IsNullOrWhiteSpace(restaurantIdStr) || !Guid.TryParse(restaurantIdStr, out var restaurantId))
    {
        return Results.BadRequest(new { message = "Missing or invalid restaurant_id claim" });
    }

    // 2. Lấy toạ độ nhà hàng từ Catalog qua Kong (giống CreateOrderFromBasketAsync)
    double restaurantLat = 10.8231;   // fallback default HCM
    double restaurantLng = 106.6297;  // fallback default HCM

    try
    {
        var kongClient = f.CreateClient("kong");

        // giống OrderBffApi: /api/catalog/restaurants
        var rRes = await kongClient.GetAsync("/api/catalog/restaurants");
        rRes.EnsureSuccessStatusCode();

        var rJson = await rRes.Content.ReadAsStringAsync();
        var restaurants = JsonSerializer.Deserialize<List<OrderBffApi.RestaurantLocationDto>>(
            rJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        ) ?? new List<OrderBffApi.RestaurantLocationDto>();

        var restaurant = restaurants.FirstOrDefault(r => r.RestaurantId == restaurantId);
        if (restaurant is not null)
        {
            restaurantLat = restaurant.Latitude;
            restaurantLng = restaurant.Longitude;
        }
    }
    catch (Exception ex)
    {
        // Có lỗi thì log + dùng default, tránh làm vỡ flow tạo drone
        Console.WriteLine($"[BFF] Cannot load restaurant location, fallback default: {ex}");
    }

    // 3. Đọc body FE gửi (chỉ cần { code })
    using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8);
    var json = await reader.ReadToEndAsync();

    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    if (!root.TryGetProperty("code", out var codeProp))
    {
        return Results.BadRequest(new { message = "code is required" });
    }

    var code = codeProp.GetString();
    if (string.IsNullOrWhiteSpace(code))
    {
        return Results.BadRequest(new { message = "code is required" });
    }

    // 4. Build payload gửi sang Delivery.API (CreateDroneRequest)
    var payload = new
    {
        Code = code,
        RestaurantId = restaurantId,
        InitialLatitude = restaurantLat,
        InitialLongitude = restaurantLng
    };

    var http = f.CreateClient("delivery");

    var res = await http.PostAsync(
        "/api/drones",
        new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
    );

    var resBody = await res.Content.ReadAsStringAsync();

    if (res.IsSuccessStatusCode)
    {
        return Results.Text(resBody, "application/json", Encoding.UTF8);
    }

    return Results.StatusCode((int)res.StatusCode);
})
.RequireAuthorization();

// PUT /bff-api/drones/{id}/status
app.MapPut("/bff-api/drones/{id:int}/status",
    async (int id, HttpContext ctx, IHttpClientFactory f) =>
    {
        var user = ctx.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        // Đọc raw body từ FE (JSON: { "status": 1 })
        using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8);
        var json = await reader.ReadToEndAsync();

        var http = f.CreateClient("delivery");

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res = await http.PutAsync($"/api/drones/{id}/status", content);

        var body = await res.Content.ReadAsStringAsync();

        // Forward status code + body về FE
        return Results.Text(body, "application/json", Encoding.UTF8, (int)res.StatusCode);
    })
   .RequireAuthorization();




app.Run();