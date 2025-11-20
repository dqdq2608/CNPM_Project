using eShop.EventBus.Abstractions;
using eShop.PaymentProcessor.IntegrationEvents.EventHandling;
using Payment.IntegrationEvents.Events;
using Payment.Providers.Abstractions;
using Payment.Providers.PayOS;
using eShop.ServiceDefaults;
using PaymentProcessor.Apis;

var builder = WebApplication.CreateBuilder(args);

// ===== eShop defaults =====
builder.AddServiceDefaults();
builder.Services.AddControllers();

// ===== EventBus + subscription =====
var eventBusBuilder = builder.AddRabbitMqEventBus("eventbus")
    .AddSubscription<
        OrderStatusChangedToStockConfirmedIntegrationEvent,
        OrderStatusChangedToStockConfirmedIntegrationEventHandler>();

// Handler DI
builder.Services.AddTransient<OrderStatusChangedToStockConfirmedIntegrationEventHandler>();

// Keyed handler cho EventBus mới
builder.Services.AddKeyedTransient<IIntegrationEventHandler, OrderStatusChangedToStockConfirmedIntegrationEventHandler>(
    typeof(OrderStatusChangedToStockConfirmedIntegrationEvent));

// ===== PayOS provider =====
builder.Services.Configure<PayOsOptions>(
    builder.Configuration.GetSection(PayOsOptions.SectionName));

builder.Services.AddHttpClient<PayOsPaymentProvider>();
builder.Services.AddTransient<IPaymentProvider, PayOsPaymentProvider>();
builder.Services.AddScoped<IPaymentApi, PaymentApi>();
builder.Services.AddSingleton<IPaymentLinkCache, InMemoryPaymentLinkCache>();
builder.Services.AddScoped<IPaymentLinkService, PaymentLinkService>();

var app = builder.Build();

// health, metrics...
app.MapDefaultEndpoints();
app.MapControllers();

app.Run();
