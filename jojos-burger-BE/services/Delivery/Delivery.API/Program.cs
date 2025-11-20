using Delivery.API.Apis;
using Delivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

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

var app = builder.Build();

// 🔹 1. AUTO MIGRATE DB KHI SERVICE START
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
    db.Database.Migrate(); // <- dòng quan trọng
}

// 🔹 2. MAP ENDPOINT
app.MapDeliveryApi();

// 🔹 3. RUN APP
app.Run();