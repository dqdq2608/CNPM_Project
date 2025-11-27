namespace Delivery.API.Dtos.Responses;

public record DeliveryResponse(
    int Id,
    int OrderId,
    double RestaurantLat,
    double RestaurantLon,
    double CustomerLat,
    double CustomerLon,
    double DistanceKm,
    decimal DeliveryFee,
    string Status);
