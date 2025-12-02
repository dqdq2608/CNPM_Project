using System.Net.Http.Json;

namespace IdentityServerBFF.Application.Models.Drone; 

public class DeliveryClient : IDeliveryClient
{
    private readonly HttpClient _httpClient;

    public DeliveryClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<DroneDto>> GetDronesAsync(CancellationToken ct = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<DroneDto>>("/api/drones", ct);
        return result ?? new List<DroneDto>();
    }

    public async Task<DroneDto> CreateDroneAsync(CreateDroneRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/drones", request, ct);
        response.EnsureSuccessStatusCode();

        var drone = await response.Content.ReadFromJsonAsync<DroneDto>(cancellationToken: ct);
        return drone!;
    }

    public async Task<DroneDto> UpdateDroneStatusAsync(int id, UpdateDroneStatusRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/drones/{id}/status", request, ct);
        response.EnsureSuccessStatusCode();

        var drone = await response.Content.ReadFromJsonAsync<DroneDto>(cancellationToken: ct);
        return drone!;
    }
}
