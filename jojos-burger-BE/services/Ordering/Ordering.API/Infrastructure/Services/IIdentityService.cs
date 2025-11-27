namespace eShop.Ordering.API.Infrastructure.Services;

public interface IIdentityService
{
    string GetUserIdentity();
    string GetRestaurantId();
    string GetUserName();
}

