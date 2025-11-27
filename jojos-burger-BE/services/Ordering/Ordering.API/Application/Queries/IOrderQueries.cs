namespace eShop.Ordering.API.Application.Queries;

public interface IOrderQueries
{
    Task<Order> GetOrderAsync(int id);

    Task<IEnumerable<OrderSummary>> GetOrdersFromUserAsync(string userId);
    Task<IEnumerable<OrderSummary>> GetOrdersFromRestaurantAsync(Guid restaurantId);
    Task<IEnumerable<CardType>> GetCardTypesAsync();
}
