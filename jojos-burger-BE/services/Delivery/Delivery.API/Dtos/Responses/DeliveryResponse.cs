namespace Delivery.API.Dtos.Responses;

public record DeliveryResponse(
    int Id,
    int OrderId,
    double DistanceKm,
    decimal DeliveryFee,
    string Status);
