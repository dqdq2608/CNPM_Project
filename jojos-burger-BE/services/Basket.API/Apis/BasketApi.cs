using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace eShop.Basket.API;

public static class BasketApi
{
    public static IEndpointRouteBuilder MapBasketApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/basket");

        // GET /api/basket/{buyerId}
        group.MapGet("/{buyerId}", async (string buyerId, IBasketRepository repo) =>
        {
            var basket = await repo.GetBasketAsync(buyerId);
            return basket is null ? Results.NotFound() : Results.Ok(basket);
        })
        .WithName("GetBasket")
        .Produces<CustomerBasket>(200)
        .Produces(404);

        // POST /api/basket
        group.MapPost("/", async (CustomerBasket basket, IBasketRepository repo) =>
        {
            var updated = await repo.UpdateBasketAsync(basket);
            return Results.Ok(updated);
        })
        .WithName("UpdateBasket")
        .Produces<CustomerBasket>(200);

        // DELETE /api/basket/{buyerId}
        group.MapDelete("/{buyerId}", async (string buyerId, IBasketRepository repo) =>
        {
            var deleted = await repo.DeleteBasketAsync(buyerId);
            return deleted ? Results.Ok() : Results.NotFound();
        })
        .WithName("DeleteBasket")
        .Produces(200)
        .Produces(404);

        return app;
    }
}
