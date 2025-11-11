// using Asp.Versioning.Builder;

// var builder = WebApplication.CreateBuilder(args);

// builder.AddServiceDefaults();
// builder.AddApplicationServices();
// builder.Services.AddProblemDetails();

// var withApiVersioning = builder.Services.AddApiVersioning();

// builder.AddDefaultOpenApi(withApiVersioning);

// var app = builder.Build();

// app.MapDefaultEndpoints();

// app.NewVersionedApi("Catalog")
//    .MapCatalogApiV1();

// app.UseDefaultOpenApi();
// app.Run();

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

