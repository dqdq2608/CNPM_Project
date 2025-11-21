using System;
using System.Text.Json.Serialization;

namespace IdentityServerBFF.Application.Services
{
    public sealed record DeliveryQuoteRequest(
        [property: JsonPropertyName("restaurantId")] Guid RestaurantId,
        [property: JsonPropertyName("deliveryAddress")] string DeliveryAddress
    );
}
