using eShop.Basket.API;
using eShop.Basket.API.Repositories;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Lấy connection string Redis (ưu tiên cấu hình, fallback localhost:6379)
var redisConnectionString =
    builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";

// Đăng ký IConnectionMultiplexer (Redis client dùng chung)
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    return ConnectionMultiplexer.Connect(redisConnectionString);
});

// Đăng ký Basket repository dùng Redis
builder.Services.AddScoped<IBasketRepository, RedisBasketRepository>();

// Swagger để test API Basket
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Map các endpoint Basket (file Apis/BasketApi.cs)
app.MapBasketApi();

app.Run();
