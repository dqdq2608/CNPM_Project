using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IdentityServerBFF.Application.Services;
using Microsoft.AspNetCore.WebUtilities;

namespace IdentityServerBFF.Infrastructure.Services;

public class CatalogBffApi : ICatalogBffApi
{
    private readonly HttpClient _http;

    // Giả định HttpClient này đã được cấu hình BaseAddress = KONG URL
    // vd: https://localhost:8443
    private const string CatalogBasePath = "/catalog";

    public CatalogBffApi(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> GetItemsAsync(
        int pageIndex,
        int pageSize,
        int? typeId,
        Guid? restaurantId,
        bool? onlyAvailable,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["pageIndex"] = pageIndex.ToString(),
            ["pageSize"] = pageSize.ToString()
        };

        if (typeId.HasValue)
            query["typeId"] = typeId.Value.ToString();

        if (restaurantId.HasValue && restaurantId.Value != Guid.Empty)
            query["restaurantId"] = restaurantId.Value.ToString();

        if (onlyAvailable == true)
            query["onlyAvailable"] = "true";

        var url = QueryHelpers.AddQueryString($"{CatalogBasePath}/items", query!);

        var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string> SearchItemsByNameAsync(
        string name,
        int pageIndex,
        int pageSize,
        int? typeId,
        Guid? restaurantId,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["pageIndex"] = pageIndex.ToString(),
            ["pageSize"] = pageSize.ToString()
        };

        if (typeId.HasValue)
            query["typeId"] = typeId.Value.ToString();

        if (restaurantId.HasValue && restaurantId.Value != Guid.Empty)
            query["restaurantId"] = restaurantId.Value.ToString();

        var url = QueryHelpers.AddQueryString(
            $"{CatalogBasePath}/items/by/{Uri.EscapeDataString(name)}",
            query!
        );

        var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string> GetItemByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var url = $"{CatalogBasePath}/items/{id}";
        var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string> GetCatalogTypesWithPicsAsync(
        CancellationToken cancellationToken = default)
    {
        var url = $"{CatalogBasePath}/catalogtypes-with-pics";
        var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string> GetRestaurantsAsync(
        CancellationToken cancellationToken = default)
    {
        var url = $"{CatalogBasePath}/restaurants";
        var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string> CreateCatalogItemAsync(
        string bodyJson,
        CancellationToken cancellationToken = default)
    {
        var url = $"{CatalogBasePath}/items";
        var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string> UpdateCatalogItemAsync(
        string bodyJson,
        CancellationToken cancellationToken = default)
    {
        var url = $"{CatalogBasePath}/items";
        var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        var response = await _http.PutAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task DeleteCatalogItemAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var url = $"{CatalogBasePath}/items/{id}";
        var response = await _http.DeleteAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> CreateCatalogTypeAsync(
        string bodyJson,
        CancellationToken cancellationToken = default)
    {
        var url = $"{CatalogBasePath}/catalogtypes";
        var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string> UpdateCatalogTypeAsync(
        string bodyJson,
        CancellationToken cancellationToken = default)
    {
        var url = $"{CatalogBasePath}/catalogtypes";
        var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        var response = await _http.PutAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task DeleteCatalogTypeAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var url = $"{CatalogBasePath}/catalogtypes/{id}";
        var response = await _http.DeleteAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
