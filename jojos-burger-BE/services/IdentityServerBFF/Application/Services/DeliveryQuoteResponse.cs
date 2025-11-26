namespace IdentityServerBFF.Application.Services
{
    public sealed record DeliveryQuoteResponse(
        double DistanceKm,
        decimal DeliveryFee,
        double OriginLat,
        double OriginLon,
        double DestLat,
        double DestLon
    );
}
