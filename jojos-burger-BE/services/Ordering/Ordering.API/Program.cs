using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

using eShop.Ordering.API.Infrastructure;
using eShop.Ordering.API.IntegrationEvents.EventHandling;
using eShop.Ordering.API.Application.IntegrationEvents;
using eShop.Ordering.API.Application.IntegrationEvents.Events;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;

using eShop.EventBus;
using eShop.EventBus.Abstractions;
using Payment.IntegrationEvents.Events;
using OrderPaymentSucceededIntegrationEvent = Payment.IntegrationEvents.Events.OrderPaymentSucceededIntegrationEvent;
using OrderPaymentFailedIntegrationEvent = Payment.IntegrationEvents.Events.OrderPaymentFailedIntegrationEvent;   // dùng event từ Payment.IntegrationEvents

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ================== DB CONTEXT ==================
builder.Services.AddDbContext<OrderingContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderingDb"));
});

builder.Services.AddTransient<IDbSeeder<OrderingContext>, OrderingContextSeed>();

// ================== AUTHENTICATION + AUTHORIZATION ==================
builder.AddDefaultAuthentication();

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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ================== EVENT BUS RABBITMQ ==================

// tạo eventbus builder
var eventBusBuilder = builder.AddRabbitMqEventBus("eventbus");

// service publish integration event từ chính Ordering (đã có sẵn trong solution)
builder.Services.AddTransient<IOrderingIntegrationEventService, OrderingIntegrationEventService>();

// đăng ký handler DI cho 2 event Payment
builder.Services.AddTransient<OrderPaymentSucceededIntegrationEventHandler>();
builder.Services.AddTransient<OrderPaymentFailedIntegrationEventHandler>();

// ⭐ KEYED HANDLERS cho EventBus mới
builder.Services.AddKeyedTransient<IIntegrationEventHandler, OrderPaymentSucceededIntegrationEventHandler>(
    typeof(OrderPaymentSucceededIntegrationEvent));

builder.Services.AddKeyedTransient<IIntegrationEventHandler, OrderPaymentFailedIntegrationEventHandler>(
    typeof(OrderPaymentFailedIntegrationEvent));

// ⭐ Đăng ký subscription (event từ Payment → Ordering)
eventBusBuilder
    .AddSubscription<OrderPaymentSucceededIntegrationEvent, OrderPaymentSucceededIntegrationEventHandler>()
    .AddSubscription<OrderPaymentFailedIntegrationEvent, OrderPaymentFailedIntegrationEventHandler>();

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

        await context.Database.MigrateAsync();
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

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();

var orders = app.NewVersionedApi("Orders");
if (app.Environment.IsDevelopment())
{
    orders.MapOrdersApiV1();
    orders.MapOrdersInternalApi();              // dev: không require token
}
else
{
    orders.MapOrdersApiV1()
          .RequireAuthorization("orders-scope");
    orders.MapOrdersInternalApi();
}

app.UseDefaultOpenApi();

app.Run();
