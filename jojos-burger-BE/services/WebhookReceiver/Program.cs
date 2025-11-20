using eShop.EventBus;
using eShop.ServiceDefaults;
using WebhookReceiver.PayOS.Endpoints;
using WebhookReceiver.PayOS.Handlers;
using WebhookReceiver.PayOS.Models;
using WebhookReceiver.PayOS.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// WebhookReceiver chỉ PUBLISH event ⇒ chỉ cần EventBus, không subscription
builder.AddRabbitMqEventBus("EventBus");

builder.Services.AddSingleton<PayOsSignatureVerifier>();
builder.Services.AddTransient<IPayOsWebhookHandler, PayOsWebhookHandler>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapPayOsWebhook();   // /webhooks/payos

app.Run();
