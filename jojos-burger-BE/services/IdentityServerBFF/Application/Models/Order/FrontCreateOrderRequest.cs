public sealed record FrontOrderItem(int Id, int Quantity);

public sealed record FrontCreateOrderRequest(
    List<FrontOrderItem> Products,

    Guid RestaurantId,        // 👈 chi nhánh người dùng chọn

    string DeliveryAddress    // 👈 địa chỉ (string) để geocoding trong BFF
);
