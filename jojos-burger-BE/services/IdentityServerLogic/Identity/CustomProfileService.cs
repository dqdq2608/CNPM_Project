using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace IdentityServerLogic.Identity;

public class CustomProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomProfileService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // ==========================
    // 1) Thêm claims vào token
    // ==========================
    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        var sub = context.Subject.GetSubjectId();
        var user = await _userManager.FindByIdAsync(sub);
        if (user == null) return;

        var claims = new List<Claim>
        {
            new Claim("name", user.FullName ?? user.UserName ?? string.Empty),
            new Claim("email", user.Email ?? string.Empty),
        };

        if (!string.IsNullOrEmpty(user.UserType))
            claims.Add(new Claim("user_type", user.UserType));

        if (!string.IsNullOrEmpty(user.RestaurantId))
            claims.Add(new Claim("restaurant_id", user.RestaurantId));

        if (!string.IsNullOrEmpty(user.RestaurantName))
            claims.Add(new Claim("restaurant_name", user.RestaurantName));

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(r => new Claim("role", r)));

        context.IssuedClaims.AddRange(claims.Where(c => !string.IsNullOrEmpty(c.Value)));
    }

    // ==========================
    // 2) Quyết định user có Active?
    // ==========================
    public async Task IsActiveAsync(IsActiveContext context)
    {
        var sub = context.Subject.GetSubjectId();
        var user = await _userManager.FindByIdAsync(sub);

        if (user == null)
        {
            context.IsActive = false;
            return;
        }

        // ---------------------
        //  A) CUSTOMER → luôn active
        // ---------------------
        if (string.IsNullOrEmpty(user.RestaurantId))
        {
            context.IsActive = true;
            return;
        }

        // ---------------------
        //  B) RESTAURANT ADMIN
        // ---------------------
        // Nếu restaurant bị đóng cửa / soft deleted
        if (!user.IsRestaurantActive || user.RestaurantStatus == "Closed")
        {
            context.IsActive = false;
            return;
        }

        // Nếu active
        context.IsActive = true;
    }
}
