using Microsoft.AspNetCore.Builder;
using CardType = eShop.Ordering.API.Application.Queries.CardType;
using Order = eShop.Ordering.API.Application.Queries.Order;
using System.Linq;
using OrderStockConfirmedEvent = Payment.IntegrationEvents.Events.OrderStatusChangedToStockConfirmedIntegrationEvent;
using eShop.EventBus.Abstractions;
using Payment.IntegrationEvents.Events; // có cũng được, nhưng alias mới sẽ được dùng


public static class OrdersApi
{
    public static RouteGroupBuilder MapOrdersApiV1(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/orders").HasApiVersion(1.0);

        api.MapPut("/cancel", CancelOrderAsync);
        api.MapPut("/ship", ShipOrderAsync);
        api.MapGet("{orderId:int}", GetOrderAsync);
        api.MapGet("/", GetOrdersByUserAsync);
        api.MapGet("/byuser/{userId}", GetOrdersByUserIdAsync);
        api.MapGet("/cardtypes", GetCardTypesAsync);
        api.MapPost("/draft", CreateOrderDraftAsync);
        api.MapPost("/", CreateOrderAsync);

        api.MapPost("/test-stock-confirmed", PublishTestStockConfirmedEventAsync);

        return api;
    }

    public static async Task<Results<Ok, BadRequest<string>, ProblemHttpResult>> CancelOrderAsync(
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancelOrderCommand command,
        [AsParameters] OrderServices services)
    {
        if (requestId == Guid.Empty)
        {
            return TypedResults.BadRequest("Empty GUID is not valid for request ID");
        }

        var requestCancelOrder = new IdentifiedCommand<CancelOrderCommand, bool>(command, requestId);

        services.Logger.LogInformation(
            "Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
            requestCancelOrder.GetGenericTypeName(),
            nameof(requestCancelOrder.Command.OrderNumber),
            requestCancelOrder.Command.OrderNumber,
            requestCancelOrder);

        var commandResult = await services.Mediator.Send(requestCancelOrder);

        if (!commandResult)
        {
            return TypedResults.Problem(detail: "Cancel order failed to process.", statusCode: 500);
        }

        return TypedResults.Ok();
    }

    public static async Task<Results<Ok, BadRequest<string>, ProblemHttpResult>> ShipOrderAsync(
        [FromHeader(Name = "x-requestid")] Guid requestId,
        ShipOrderCommand command,
        [AsParameters] OrderServices services)
    {
        if (requestId == Guid.Empty)
        {
            return TypedResults.BadRequest("Empty GUID is not valid for request ID");
        }

        var requestShipOrder = new IdentifiedCommand<ShipOrderCommand, bool>(command, requestId);

        services.Logger.LogInformation(
            "Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
            requestShipOrder.GetGenericTypeName(),
            nameof(requestShipOrder.Command.OrderNumber),
            requestShipOrder.Command.OrderNumber,
            requestShipOrder);

        var commandResult = await services.Mediator.Send(requestShipOrder);

        if (!commandResult)
        {
            return TypedResults.Problem(detail: "Ship order failed to process.", statusCode: 500);
        }

        return TypedResults.Ok();
    }

    public static async Task<Results<Ok<Order>, NotFound>> GetOrderAsync(int orderId, [AsParameters] OrderServices services)
    {
        try
        {
            var order = await services.Queries.GetOrderAsync(orderId);
            return TypedResults.Ok(order);
        }
        catch
        {
            return TypedResults.NotFound();
        }
    }

    public static async Task<Ok<IEnumerable<OrderSummary>>> GetOrdersByUserAsync([AsParameters] OrderServices services)
    {
        var userId = services.IdentityService.GetUserIdentity();
        var orders = await services.Queries.GetOrdersFromUserAsync(userId);
        return TypedResults.Ok(orders);
    }

    public static async Task<Ok<IEnumerable<OrderSummary>>> GetOrdersByUserIdAsync(
    string userId,
    [AsParameters] OrderServices services)
    {
        // Không dùng IdentityService nữa, dùng trực tiếp userId từ route
        var orders = await services.Queries.GetOrdersFromUserAsync(userId);
        return TypedResults.Ok(orders);
    }


    public static async Task<Ok<IEnumerable<CardType>>> GetCardTypesAsync(IOrderQueries orderQueries)
    {
        var cardTypes = await orderQueries.GetCardTypesAsync();
        return TypedResults.Ok(cardTypes);
    }

    public static async Task<OrderDraftDTO> CreateOrderDraftAsync(CreateOrderDraftCommand command, [AsParameters] OrderServices services)
    {
        services.Logger.LogInformation(
            "Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
            command.GetGenericTypeName(),
            nameof(command.BuyerId),
            command.BuyerId,
            command);

        return await services.Mediator.Send(command);
    }

    public static async Task<Results<Ok<object>, BadRequest<string>>> CreateOrderAsync(
    [FromHeader(Name = "x-requestid")] Guid requestId,
    CreateOrderRequest request,
    [AsParameters] OrderServices services)
    {
        services.Logger.LogInformation(
            "CreateOrder requested. RequestId={RequestId}, UserId={UserId}, UserName={UserName}, Card={Card}",
            requestId,
            request.UserId,
            request.UserName,
            maskedCCNumber);

        // ====== build command dùng userId trong body ======
        var city    = string.IsNullOrWhiteSpace(request.City)    ? "OnlineCity"    : request.City;
        var street  = string.IsNullOrWhiteSpace(request.Street)  ? "OnlineStreet"  : request.Street;
        var state   = string.IsNullOrWhiteSpace(request.State)   ? "OnlineState"   : request.State;
        var country = string.IsNullOrWhiteSpace(request.Country) ? "VN"            : request.Country;
        var zip     = string.IsNullOrWhiteSpace(request.ZipCode) ? "00000"         : request.ZipCode;

        // card fake cho flow online (vì PayOS mới là nơi thanh toán thật)
        var cardNumber = string.IsNullOrWhiteSpace(request.CardNumber)
            ? "4111111111111"  // 13 chữ số để pass rule 12–19
            : request.CardNumber;

        var cardHolder = string.IsNullOrWhiteSpace(request.CardHolderName)
            ? (request.UserName ?? "ONLINE_USER")
            : request.CardHolderName;

        var cardSec = string.IsNullOrWhiteSpace(request.CardSecurityNumber)
            ? "000"   // 3 ký tự để pass rule
            : request.CardSecurityNumber;

        var cardTypeId = request.CardTypeId ?? 1;
        var cardExp    = request.CardExpiration ?? DateTime.UtcNow.AddYears(3);

        var createOrderCommand = new CreateOrderCommand(
            request.Items,
            request.UserId,
            request.UserName ?? string.Empty,
            city,
            street,
            state,
            country,
            zip,
            cardNumber,
            cardHolder,
            cardExp,
            cardSec,
            cardTypeId);

        var identified = new IdentifiedCommand<CreateOrderCommand, bool>(createOrderCommand, requestId);

        services.Logger.LogInformation(
            "Sending command: {CommandName} - Id: {CommandId}",
            identified.GetGenericTypeName(),
            identified.Id);

        var result = await services.Mediator.Send(identified);

        if (!result)
        {
            services.Logger.LogWarning("CreateOrderCommand failed - RequestId: {RequestId}", requestId);
            return TypedResults.BadRequest("CreateOrder failed");
        }

        using (services.Logger.BeginScope(new List<KeyValuePair<string, object>>
           { new("IdentifiedCommandId", requestId) }))
        {
            var maskedCCNumber = request.CardNumber
                .Substring(request.CardNumber.Length - 4)
                .PadLeft(request.CardNumber.Length, 'X');

            var createOrderCommand = new CreateOrderCommand(
                request.Items,
                request.UserId,
                request.UserName,
                request.City,
                request.Street,
                request.State,
                request.Country,
                request.ZipCode,
                maskedCCNumber,
                request.CardHolderName,
                request.CardExpiration,
                request.CardSecurityNumber,
                request.CardTypeId,
                request.DeliveryFee);

            var requestCreateOrder = new IdentifiedCommand<CreateOrderCommand, bool>(
                createOrderCommand,
                requestId);

            services.Logger.LogInformation(
                "Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
                requestCreateOrder.GetGenericTypeName(),
                nameof(requestCreateOrder.Id),
                requestCreateOrder.Id,
                requestCreateOrder);

            var result = await services.Mediator.Send(requestCreateOrder);

            if (!result)
            {
                services.Logger.LogWarning(
                    "CreateOrderCommand failed - RequestId: {RequestId}",
                    requestId);

                // Có thể trả BadRequest/Problem, tuỳ bạn, tạm thời trả BadRequest
                return TypedResults.BadRequest("CreateOrderCommand failed.");
            }

            services.Logger.LogInformation(
                "CreateOrderCommand succeeded - RequestId: {RequestId}",
                requestId);

            // 🔹 Sau khi tạo thành công, query lại order của user để lấy orderId mới nhất
            try
            {
                var orders = await services.Queries.GetOrdersFromUserAsync(request.UserId);
                var lastOrder = orders
                    .OrderByDescending(o => o.Date)
                    .FirstOrDefault();

                if (lastOrder is null)
                {
                    services.Logger.LogWarning(
                        "No orders found for user {UserId} after CreateOrderCommand succeeded.",
                        request.UserId);

                    // fallback: orderId = 0
                    return TypedResults.Ok<object>(new { orderId = 0 });
                }

                // OrderNumber chính là Id mà FE/BFF dùng
                return TypedResults.Ok<object>(new { orderId = lastOrder.OrderNumber });
            }
            catch (Exception ex)
            {
                services.Logger.LogError(
                    ex,
                    "Error when trying to load last order for user {UserId} after CreateOrderCommand succeeded.",
                    request.UserId);

                // fallback an toàn
                return TypedResults.Ok<object>(new { orderId = 0 });
            }
        }

        var response = new CreateOrderResponse(
            createdOrder.OrderNumber,
            (decimal)createdOrder.Total);

        return TypedResults.Ok(response);
    }

    // Response trả về
    public record CreateOrderResponse(int OrderId, decimal Total);


    public static async Task<Ok> PublishTestStockConfirmedEventAsync(
        IEventBus eventBus,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("OrdersApi.TestStockConfirmed");

        var evt = new OrderStockConfirmedEvent
        {
            OrderId           = 5678,                          // order giả
            OrderStatus       = "StockConfirmed",
            BuyerName         = "Ordering Test Buyer",
            BuyerIdentityGuid = Guid.NewGuid().ToString()
        };

        logger.LogInformation(
            ">>> [ORDERING-TEST] Publishing OrderStatusChangedToStockConfirmedIntegrationEvent for OrderId {OrderId}",
            evt.OrderId);

        await eventBus.PublishAsync(evt);

        logger.LogInformation(
            ">>> [ORDERING-TEST] Published event for OrderId {OrderId}",
            evt.OrderId);

        return TypedResults.Ok();
    }


}

public record CreateOrderRequest(
    string UserId,
    string UserName,
    string City,
    string Street,
    string State,
    string Country,
    string ZipCode,
    string CardNumber,
    string CardHolderName,
    DateTime CardExpiration,
    string CardSecurityNumber,
    int CardTypeId,
    string Buyer,
    List<BasketItem> Items,
    decimal DeliveryFee);
