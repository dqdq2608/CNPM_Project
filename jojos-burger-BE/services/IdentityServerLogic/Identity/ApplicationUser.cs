using Microsoft.AspNetCore.Identity;

namespace IdentityServerLogic.Identity;
public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }

    public string? RestaurantId { get; set; }
    public string? RestaurantName { get; set; }

    public string? UserType { get; set; } // Customer / RestaurantAdmin

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRestaurantActive { get; set; } = true; // cho phép login hay không
    public string RestaurantStatus { get; set; } = "Active"; // "Active" | "Inactive" | "Closed"
}
