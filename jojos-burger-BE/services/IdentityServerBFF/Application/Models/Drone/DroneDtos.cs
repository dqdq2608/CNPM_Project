namespace IdentityServerBFF.Application.Models.Drone; // sửa namespace cho đúng project của bạn

public class DroneDto
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public int Status { get; set; }          // enum numeric từ BE
    public double CurrentLatitude { get; set; }
    public double CurrentLongitude { get; set; }
    public DateTime LastHeartbeatAt { get; set; }
}

public class CreateDroneRequest
{
    public string Code { get; set; } = default!;
    public double InitialLatitude { get; set; }
    public double InitialLongitude { get; set; }
}

public class UpdateDroneStatusRequest
{
    public int Status { get; set; } // Idle / Delivering / ...
}

public interface IDeliveryClient
{
    Task<IReadOnlyList<DroneDto>> GetDronesAsync(CancellationToken ct = default);
    Task<DroneDto> CreateDroneAsync(CreateDroneRequest request, CancellationToken ct = default);
    Task<DroneDto> UpdateDroneStatusAsync(int id, UpdateDroneStatusRequest request, CancellationToken ct = default);
}
