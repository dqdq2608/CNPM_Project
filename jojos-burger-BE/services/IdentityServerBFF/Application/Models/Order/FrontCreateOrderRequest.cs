using System.Text.Json.Serialization;

public sealed record FrontOrderItem(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("quantity")] int Quantity
);

public sealed record FrontCreateOrderRequest(
    [property: JsonPropertyName("products")] List<FrontOrderItem> Products,

    [property: JsonPropertyName("restaurantId")] Guid RestaurantId,

    [property: JsonPropertyName("deliveryAddress")] string DeliveryAddress
);
