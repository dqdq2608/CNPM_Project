namespace Delivery.Domain.Drone;

public class Drone
{
    public int Id { get; set; }
    // Mã định danh cho nhà hàng sở hữu drone
    public Guid RestaurantId { get; set; }
    public string Code { get; set; } = default!;

    public DroneStatus Status { get; set; } = DroneStatus.Idle;

    public double CurrentLatitude { get; set; }
    public double CurrentLongitude { get; set; }

    public DateTime LastHeartbeatAt { get; set; } = DateTime.UtcNow;
    public void UpdateLocation(double lat, double lng)
    {
        CurrentLatitude = lat;
        CurrentLongitude = lng;
        LastHeartbeatAt = DateTime.UtcNow;
    }

    public void SetStatus(DroneStatus status)
    {
        Status = status;
        LastHeartbeatAt = DateTime.UtcNow;
    }
}
