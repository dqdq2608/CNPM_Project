var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddDefaultPolicy(p => 
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

// Mock dữ liệu sản phẩm
var products = new[]
{
    new { id = 1, name = "Classic Burger", price = 5.5 },
    new { id = 2, name = "Cheese Burger",  price = 6.0 },
    new { id = 3, name = "Bacon Burger",   price = 6.5 },
};

// Endpoint kiểm tra
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "catalog" }));

// Endpoint chính - lấy danh sách sản phẩm
app.MapGet("/api/catalog/items", () => Results.Ok(products));

// Chạy trên cổng 6001
app.Run("http://0.0.0.0:6001");
