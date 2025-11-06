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

// 🔹 ServiceDefaults (Health checks, Logging, v.v.)
builder.AddServiceDefaults();

// 🔹 Catalog-specific services
builder.AddApplicationServices();

// 🔹 ProblemDetails middleware (error format)
builder.Services.AddProblemDetails();

// 🔹 Swagger cơ bản (không versioning)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 🔹 Map health endpoints (từ ServiceDefaults)
app.MapDefaultEndpoints();

// 🔹 Bật Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

// 🔹 Map các API endpoints chính
app.MapCatalogApiV1();

app.Run();
