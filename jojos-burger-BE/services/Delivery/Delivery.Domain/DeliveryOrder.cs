using System;

namespace Delivery.Domain;

public class DeliveryOrder
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public double RestaurantLat { get; set; }
    public double RestaurantLon { get; set; }

    public double CustomerLat { get; set; }
    public double CustomerLon { get; set; }

    public double DistanceKm { get; set; }
    public decimal DeliveryFee { get; set; }

    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public double? DroneLat { get; set; }
    public double? DroneLon { get; set; }
}
