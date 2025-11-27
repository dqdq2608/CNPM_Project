namespace eShop.Ordering.API.Infrastructure.Services;

public class IdentityService(IHttpContextAccessor context) : IIdentityService
{
    public string GetUserIdentity()
        => context.HttpContext?.User.FindFirst("sub")?.Value;

    public string GetRestaurantId()
        => context.HttpContext?.User.FindFirst("restaurant_id")?.Value;
    public string GetUserName()
        => context.HttpContext?.User.Identity?.Name;
}
