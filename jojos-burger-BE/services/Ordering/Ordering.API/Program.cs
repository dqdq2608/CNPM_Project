using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// -------------------- DB CONTEXT (SQLite) --------------------
builder.Services.AddDbContext<OrderingContext>(options =>
{
    options.UseInMemoryDatabase("OrderingDB");
});

// -------------------- SERVICE DEFAULTS --------------------
builder.AddServiceDefaults();
builder.AddApplicationServices();
builder.Services.AddProblemDetails();

// -------------------- API VERSIONING --------------------
builder.Services.AddApiVersioning()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'V";
        options.SubstituteApiVersionInUrl = true;
    });

builder.AddDefaultOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -------------------- EVENT BUS RABBITMQ --------------------
// Đăng ký EventBus + RabbitMQ client, trả về IEventBusBuilder để add subscription
var eventBusBuilder = builder.AddRabbitMqEventBus("eventbus");

// Đăng ký các handler vào DI
builder.Services.AddTransient<IOrderingIntegrationEventService, OrderingIntegrationEventService>();
builder.Services.AddTransient<OrderPaymentSucceededIntegrationEventHandler>();
builder.Services.AddTransient<OrderPaymentFailedIntegrationEventHandler>();

// Khai báo subscription cho 2 event thanh toán
eventBusBuilder.AddSubscription<OrderPaymentSucceededIntegrationEvent, OrderPaymentSucceededIntegrationEventHandler>();
eventBusBuilder.AddSubscription<OrderPaymentFailedIntegrationEvent, OrderPaymentFailedIntegrationEventHandler>();

// ---------------------- BUILD APP ----------------------
var app = builder.Build();

// ---------------------- MIGRATE + SEED DB ----------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderingContext>();

    var seeder = new OrderingContextSeed();
    await seeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ---------------------- ENDPOINTS ----------------------
app.MapDefaultEndpoints();

var orders = app.NewVersionedApi("Orders");

orders.MapOrdersApiV1()
      .RequireAuthorization();

app.UseDefaultOpenApi();

app.Run();
