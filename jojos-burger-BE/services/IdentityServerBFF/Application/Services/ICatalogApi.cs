using System;

namespace IdentityServerBFF.Application.Services;

public interface ICatalogBffApi
{
    // GET /api/catalog/items
    Task<string> GetItemsAsync(
        int pageIndex,
        int pageSize,
        int? typeId,
        Guid? restaurantId,
        bool? onlyAvailable,
        CancellationToken cancellationToken = default);

    // GET /api/catalog/items/by/{name}
    Task<string> SearchItemsByNameAsync(
        string name,
        int pageIndex,
        int pageSize,
        int? typeId,
        Guid? restaurantId,
        CancellationToken cancellationToken = default);

    // GET /api/catalog/items/{id}
    Task<string> GetItemByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    // GET /api/catalog/catalogtypes-with-pics
    Task<string> GetCatalogTypesWithPicsAsync(
        CancellationToken cancellationToken = default);

    // GET /api/catalog/restaurants
    Task<string> GetRestaurantsAsync(
        CancellationToken cancellationToken = default);

    // POST /api/catalog/items
    Task<string> CreateCatalogItemAsync(
        string bodyJson,
        CancellationToken cancellationToken = default);

    // PUT /api/catalog/items
    Task<string> UpdateCatalogItemAsync(
        string bodyJson,
        CancellationToken cancellationToken = default);

    // DELETE /api/catalog/items/{id}
    Task DeleteCatalogItemAsync(
        int id,
        CancellationToken cancellationToken = default);

    // POST /api/catalog/catalogtypes
    Task<string> CreateCatalogTypeAsync(
        string bodyJson,
        CancellationToken cancellationToken = default);

    // PUT /api/catalog/catalogtypes
    Task<string> UpdateCatalogTypeAsync(
        string bodyJson,
        CancellationToken cancellationToken = default);

    // DELETE /api/catalog/catalogtypes/{id}
    Task DeleteCatalogTypeAsync(
        int id,
        CancellationToken cancellationToken = default);
}
