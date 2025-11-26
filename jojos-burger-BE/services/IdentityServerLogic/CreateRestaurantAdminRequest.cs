namespace IdentityServerLogic;

public sealed class CreateRestaurantAdminRequest
{
    public string RestaurantId { get; set; } = default!;   // ví dụ "rest-004"
    public string RestaurantName { get; set; } = default!; // ví dụ "Jojo Burger Q4"
    public string Email { get; set; } = default!;          // ví dụ "owner.rest-004@gmail.com"
}
