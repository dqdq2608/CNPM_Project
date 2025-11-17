using eShop.EventBus;
using eShop.ServiceDefaults;
using WebhookReceiver.PayOS.Endpoints;
using WebhookReceiver.PayOS.Handlers;
using WebhookReceiver.PayOS.Services;

var builder = WebApplication.CreateBuilder(args);

// eShop defaults: logging, tracing, health, config...
builder.AddServiceDefaults();

// Đăng ký EventBus với RabbitMQ (y chang các service khác)
builder.AddRabbitMqEventBus("EventBus");

// Đăng ký DI cho PayOS webhook
builder.Services.AddSingleton<PayOsSignatureVerifier>();
builder.Services.AddTransient<PayOsWebhookHandler>();

var app = builder.Build();

// Health check, metrics...
app.MapDefaultEndpoints();

// Map endpoint webhook PayOS
app.MapPayOsWebhook();

app.Run();
