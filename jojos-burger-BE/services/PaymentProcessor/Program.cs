using eShop.PaymentProcessor.IntegrationEvents.EventHandling;
using eShop.PaymentProcessor.IntegrationEvents.Events;
using eShop.ServiceDefaults;
using Payment.Providers.Abstractions;
using Payment.Providers.PayOS;

var builder = WebApplication.CreateBuilder(args);

// Các extension này nằm trong eShop.ServiceDefaults
builder.AddServiceDefaults();

// Đăng ký EventBus + subscription cho handler thanh toán
builder.AddRabbitMqEventBus("EventBus")
       .AddSubscription<OrderStatusChangedToStockConfirmedIntegrationEvent,
                        OrderStatusChangedToStockConfirmedIntegrationEventHandler>();

// Cấu hình PayOS
builder.Services.Configure<PayOsOptions>(
    builder.Configuration.GetSection(PayOsOptions.SectionName));

// HttpClient cho PayOsPaymentProvider
builder.Services.AddHttpClient<PayOsPaymentProvider>();

// Đăng ký IPaymentProvider = PayOsPaymentProvider
builder.Services.AddTransient<IPaymentProvider, PayOsPaymentProvider>();

var app = builder.Build();

// Map health/check, metrics, v.v. (cũng từ eShop.ServiceDefaults)
app.MapDefaultEndpoints();

// Endpoint test PayOS (giúp bạn test provider riêng lẻ)
app.MapPost("/test/payos", async (IPaymentProvider paymentProvider) =>
{
    var orderId = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

    var order = new OrderPaymentData
    {
        OrderId     = orderId,
        Amount      = 10_000, // 10k VND
        Description = $"Test đơn hàng {orderId}",
        ReturnUrl   = "https://google.com",
        CancelUrl   = "https://google.com"
    };

    var result = await paymentProvider.CreatePaymentAsync(order);
    return Results.Ok(result);
});

app.Run();
