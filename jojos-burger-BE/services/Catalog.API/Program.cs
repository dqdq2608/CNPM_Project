using eShop.Catalog.API;
using eShop.Catalog.API.Extensions;
using eShop.Catalog.API.Services;

var builder = WebApplication.CreateBuilder(args);

// CORS
var AllowFrontend = "AllowFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(AllowFrontend, policy =>
    {
        policy.WithOrigins("https://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // cần cho credentials
    });
});

// Service defaults / logging / health, v.v.
builder.AddServiceDefaults();
builder.AddApplicationServices();

builder.Services.AddProblemDetails();

builder.Services.AddHttpClient("ids-admin-api", client =>
{
    // gọi theo tên service trong docker network
    client.BaseAddress = new Uri("https://ids:5001");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // ⚠️ chỉ dùng cho môi trường dev
    ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

builder.Services.AddHttpClient("ordering-api", c =>
{
    c.BaseAddress = new Uri("http://ordering-api:8080"); 
    // tuỳ docker-compose, có thể là "http://ordering-api:80"
});


// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseCors(AllowFrontend); // chỉ gọi 1 lần, trước endpoints

// Health endpoints
app.MapDefaultEndpoints();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

// Map API
app.MapCatalogApiV1();

app.Run();

