using IdentityServerBFF.Endpoints;

public static class BffPublicApi
{
    public static IEndpointRouteBuilder MapBffPublicApi(this IEndpointRouteBuilder endpoints)
    {
        var bff = endpoints.MapGroup("/api")
                           .RequireAuthorization(); // hoặc bỏ nếu bạn muốn public

        // DOMAIN: Catalog
        bff.MapCatalogEndpoints();
        bff.MapCheckoutEndpoints();
        bff.MapPaymentEndpoints();

        // Sau này thêm:
        // bff.MapOrderEndpoints();
        // bff.MapBasketEndpoints();
        // bff.MapRestaurantEndpoints();
        // ...

        return endpoints;
    }
}
