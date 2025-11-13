namespace eShop.Basket.API;

public static class BasketApi
{
    public static IEndpointRouteBuilder MapBasketApi(this IEndpointRouteBuilder app)
    {
        // Nhóm chung: /api/basket
        var group = app.MapGroup("/api/basket");

        // TODO (sau này): yêu cầu login
        // group.RequireAuthorization();

        // ================================
        // 1. API dùng buyerId trực tiếp (debug / admin)
        // ================================

        // GET /api/basket/{buyerId}
        group.MapGet("/{buyerId}", async (string buyerId, IBasketRepository repo) =>
        {
            var basket = await repo.GetBasketAsync(buyerId);

            if (basket is null)
            {
                basket = new CustomerBasket(buyerId)
                {
                    Items = new List<eShop.Basket.API.Model.BasketItem>()
                };
            }

            return Results.Ok(basket);
        });

        // DELETE /api/basket/{buyerId}
        group.MapDelete("/{buyerId}", async (string buyerId, IBasketRepository repo) =>
        {
            var deleted = await repo.DeleteBasketAsync(buyerId);
            return deleted ? Results.Ok() : Results.NotFound();
        })
        .WithName("DeleteBasketById")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // ================================
        // 2. API dùng user hiện tại (claim "sub")
        //    -> FE nên dùng nhóm này về lâu dài
        // ================================

        // GET /api/basket  (lấy giỏ của user hiện tại)
        group.MapGet("/", async (HttpContext http, IBasketRepository repo) =>
        {
            var buyerId = GetBuyerId(http);
            var basket = await repo.GetBasketAsync(buyerId)
                        ?? new CustomerBasket(buyerId);

            return Results.Ok(basket);
        })
        .WithName("GetMyBasket")
        .Produces<CustomerBasket>(StatusCodes.Status200OK);

        // POST /api/basket  (update giỏ cho user hiện tại)
        // FE gửi body: { items: [...] } là đủ, BuyerId BE tự set
        group.MapPost("/", async (HttpContext http, CustomerBasket basket, IBasketRepository repo) =>
        {
            // đảm bảo BuyerId = user hiện tại, không tin dữ liệu FE
            var buyerId = GetBuyerId(http);
            basket.BuyerId = buyerId;

            var updated = await repo.UpdateBasketAsync(basket);
            return Results.Ok(updated);
        })
        .WithName("UpdateBasket")
        .Produces<CustomerBasket>(200);


        // DELETE /api/basket  (xóa giỏ user hiện tại)
        group.MapDelete("/", async (HttpContext http, IBasketRepository repo) =>
        {
            var buyerId = GetBuyerId(http);
            var deleted = await repo.DeleteBasketAsync(buyerId);
            return deleted ? Results.Ok() : Results.NotFound();
        })
        .WithName("DeleteMyBasket")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    // Helper: lấy BuyerId từ claim "sub"
    private static string GetBuyerId(HttpContext http)
    {
        // 1. Ưu tiên header do BFF forward
        var fromHeader = http.Request.Headers["X-User-Sub"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(fromHeader))
        {
            return fromHeader;
        }

        // 2. Fallback: nếu sau này bạn dùng JWT trực tiếp thì vẫn dùng claim sub
        var fromClaim = http.User.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(fromClaim))
        {
            return fromClaim;
        }

        throw new InvalidOperationException("Không tìm thấy buyer id (X-User-Sub hoặc claim sub).");
    }

}
