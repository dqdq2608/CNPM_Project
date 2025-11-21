namespace IdentityServerBFF.Application.Services
{
    public sealed record DeliveryQuoteResponse(
        double DistanceKm,
        decimal DeliveryFee
    );
}
