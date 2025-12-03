using Delivery.API.Apis;
using Delivery.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Delivery.API.Clients;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký DbContext sử dụng PostgreSQL
builder.Services.AddDbContext<DeliveryDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DeliveryDb"),
        npgsql =>
        {
            // Lưu lịch sử migrations trong schema "delivery"
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "delivery");
        });
});

builder.Services.AddHttpClient<IOrderingClient, OrderingClient>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    var baseUrl = config["Ordering:BaseUrl"] ?? "http://ordering-api";

    client.BaseAddress = new Uri(baseUrl);
});


var app = builder.Build();

// 🔹 2. MAP ENDPOINT
app.MapDeliveryApi();
app.MapDronesApi();
app.MapDroneAssignmentsApi();
// 🔹 3. RUN APP
app.Run();