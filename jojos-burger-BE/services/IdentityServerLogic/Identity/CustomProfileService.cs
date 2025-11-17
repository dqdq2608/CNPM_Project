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

    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        // sub = Id của user trong AspNetUsers
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

        // role từ ASP.NET Identity
        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(r => new Claim("role", r)));

        // Thêm vào danh sách claim được issue
        context.IssuedClaims.AddRange(claims.Where(c => !string.IsNullOrEmpty(c.Value)));
    }

    public Task IsActiveAsync(IsActiveContext context)
    {
        context.IsActive = true;
        return Task.CompletedTask;
    }
}
