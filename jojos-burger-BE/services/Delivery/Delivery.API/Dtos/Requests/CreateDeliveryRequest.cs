namespace Delivery.API.Dtos.Requests;

public record CreateDeliveryRequest(
    int OrderId,
    double RestaurantLat,
    double RestaurantLon,
    double CustomerLat,
    double CustomerLon);
