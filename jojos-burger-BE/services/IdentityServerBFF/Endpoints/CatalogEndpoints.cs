using System.IO;
using System.Text;
using IdentityServerBFF.Application.Services;
using Microsoft.AspNetCore.Http;

namespace IdentityServerBFF.Endpoints;

public static class CatalogEndpoints
{
    public static RouteGroupBuilder MapCatalogEndpoints(this RouteGroupBuilder group)
    {
        var catalog = group.MapGroup("/catalog");

        // ====== Các endpoint tương ứng với catalog.js ======

        // GET /api/catalog/catalogtypes-with-pics
        catalog.MapGet("/catalogtypes-with-pics", async (
            ICatalogBffApi api,
            CancellationToken ct) =>
        {
            var json = await api.GetCatalogTypesWithPicsAsync(ct);
            return Results.Content(json, "application/json");
        });

        // POST /api/catalog/catalogtypes
        catalog.MapPost("/catalogtypes", async (
            HttpRequest request,
            ICatalogBffApi api,
            CancellationToken ct) =>
        {
            var bodyJson = await new StreamReader(request.Body, Encoding.UTF8).ReadToEndAsync(ct);
            var json = await api.CreateCatalogTypeAsync(bodyJson, ct);
            return Results.Content(json, "application/json");
        });

        // PUT /api/catalog/catalogtypes
        catalog.MapPut("/catalogtypes", async (
            HttpRequest request,
            ICatalogBffApi api,
            CancellationToken ct) =>
        {
            var bodyJson = await new StreamReader(request.Body, Encoding.UTF8).ReadToEndAsync(ct);
            var json = await api.UpdateCatalogTypeAsync(bodyJson, ct);
            return Results.Content(json, "application/json");
        });

        // DELETE /api/catalog/catalogtypes/{id}
        catalog.MapDelete("/catalogtypes/{id:int}", async (
            int id,
            ICatalogBffApi api,
            CancellationToken ct) =>
        {
            await api.DeleteCatalogTypeAsync(id, ct);
            return Results.NoContent();
        });

        // GET /api/catalog/restaurants
        catalog.MapGet("/restaurants", async (
            ICatalogBffApi api,
            CancellationToken ct) =>
        {
            var json = await api.GetRestaurantsAsync(ct);
            return Results.Content(json, "application/json");
        });

        // GET /api/catalog/items  (list + filter)
        catalog.MapGet("/items", async (
            int pageIndex,
            int pageSize,
            int? typeId,
            Guid? restaurantId,
            bool? onlyAvailable,
            ICatalogBffApi api,
            CancellationToken ct) =>
        {
            var json = await api.GetItemsAsync(pageIndex, pageSize, typeId, restaurantId, onlyAvailable, ct);
            return Results.Content(json, "application/json");
        });

        // GET /api/catalog/items/by/{name}
        catalog.MapGet("/items/by/{name}", async (
            string name,
            int pageIndex,
            int pageSize,
            int? typeId,
            Guid? restaurantId,
            ICatalogBffApi api,
            CancellationToken ct) =>
        {
            var json = await api.SearchItemsByNameAsync(name, pageIndex, pageSize, typeId, restaurantId, ct);
            return Results.Content(json, "application/json");
        });

        // GET /api/catalog/items/{id}
        catalog.MapGet("/items/{id:int}", async (
            int id,
            ICatalogBffApi api,
            CancellationToken ct) =>
        {
            var json = await api.GetItemByIdAsync(id, ct);
            return Results.Content(json, "application/json");
        });

        // POST /-api/catalog/items
        catalog.MapPost("/items", async (
            HttpRequest request,
            ICatalogBffApi api,
            CancellationToken ct) =>
        {
            var bodyJson = await new StreamReader(request.Body, Encoding.UTF8).ReadToEndAsync(ct);
            var json = await api.CreateCatalogItemAsync(bodyJson, ct);
            return Results.Content(json, "application/json");
        });

        // PUT /api/catalog/items
        catalog.MapPut("/items", async (
            HttpRequest request,
            ICatalogBffApi api,
            CancellationToken ct) =>
        {
            var bodyJson = await new StreamReader(request.Body, Encoding.UTF8).ReadToEndAsync(ct);
            var json = await api.UpdateCatalogItemAsync(bodyJson, ct);
            return Results.Content(json, "application/json");
        });

        // DELETE /api/catalog/items/{id}
        catalog.MapDelete("/items/{id:int}", async (
            int id,
            ICatalogBffApi api,
            CancellationToken ct) =>
        {
            await api.DeleteCatalogItemAsync(id, ct);
            return Results.NoContent();
        });

        return catalog;
    }
}
