using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using eShop.Ordering.API.Infrastructure;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);

// ================== DB CONTEXT ==================
builder.Services.AddDbContext<OrderingContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderingDb"));
});

builder.Services.AddTransient<IDbSeeder<OrderingContext>, OrderingContextSeed>();

// ================== AUTHENTICATION + AUTHORIZATION ==================
// DÙNG EXTENSION DÙNG CHUNG CHO CẢ HỆ THỐNG
builder.AddDefaultAuthentication();

// Policy riêng cho Ordering: bắt buộc có scope "orders"
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("orders-scope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "orders");
    });
});

// ================== SERVICE CỦA ORDERING ==================
builder.AddApplicationServices();
builder.Services.AddProblemDetails();

// ================== API VERSIONING + OPENAPI ==================
var withApiVersioning = builder.Services.AddApiVersioning();
builder.AddDefaultOpenApi();

// (nếu bạn muốn Swagger UI dev)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ================== EVENT BUS RABBITMQ ==================
var eventBusBuilder = builder.AddRabbitMqEventBus("eventbus");

builder.Services.AddTransient<IOrderingIntegrationEventService, OrderingIntegrationEventService>();
builder.Services.AddTransient<OrderPaymentSucceededIntegrationEventHandler>();
builder.Services.AddTransient<OrderPaymentFailedIntegrationEventHandler>();

eventBusBuilder.AddSubscription<OrderPaymentSucceededIntegrationEvent, OrderPaymentSucceededIntegrationEventHandler>();
eventBusBuilder.AddSubscription<OrderPaymentFailedIntegrationEvent, OrderPaymentFailedIntegrationEventHandler>();

// ================== BUILD APP ==================
var app = builder.Build();

// SEED DB
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<OrderingContext>();
        var seeder = services.GetRequiredService<IDbSeeder<OrderingContext>>();

        // Đảm bảo đã apply migrations (với DB relational trong Docker)
        await context.Database.MigrateAsync();

        // Gọi seed
        await seeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the Ordering DB.");
    }
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ⚠️ RẤT QUAN TRỌNG: bật middleware auth
app.UseAuthentication();
app.UseAuthorization();

// ENDPOINTS
app.MapDefaultEndpoints();

var orders = app.NewVersionedApi("Orders");
orders.MapOrdersApiV1();

app.UseDefaultOpenApi();

app.Run();
