using Microsoft.AspNetCore.Builder;
using CardType = eShop.Ordering.API.Application.Queries.CardType;
using Order = eShop.Ordering.API.Application.Queries.Order;
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

    public static async Task<Results<Ok<CreateOrderResponse>, BadRequest<string>>> CreateOrderAsync(
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CreateOrderRequest request,
        [AsParameters] OrderServices services)
    {
        // ====== validate nhẹ ======
        if (requestId == Guid.Empty)
        {
            services.Logger.LogWarning("Invalid request: x-requestid is empty");
            return TypedResults.BadRequest("RequestId is missing.");
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            services.Logger.LogWarning("Invalid request: UserId is empty");
            return TypedResults.BadRequest("UserId is required.");
        }

        if (request.Items == null || !request.Items.Any() || request.Items.Any(i => i.Quantity <= 0))
        {
            services.Logger.LogWarning("Invalid order items for UserId={UserId}", request.UserId);
            return TypedResults.BadRequest("Invalid order items.");
        }

        // ====== log thông tin cơ bản ======
        var maskedCCNumber = request.CardNumber.Length >= 4
            ? request.CardNumber[^4..].PadLeft(request.CardNumber.Length, 'X')
            : "***";

        services.Logger.LogInformation(
            "CreateOrder requested. RequestId={RequestId}, UserId={UserId}, UserName={UserName}, Card={Card}",
            requestId,
            request.UserId,
            request.UserName,
            maskedCCNumber);

        // ====== build command dùng userId trong body ======
        var createOrderCommand = new CreateOrderCommand(
            request.Items,
            request.UserId,
            request.UserName,
            request.City,
            request.Street,
            request.State,
            request.Country,
            request.ZipCode,
            request.CardNumber,
            request.CardHolderName,
            request.CardExpiration,
            request.CardSecurityNumber,
            request.CardTypeId);

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

        services.Logger.LogInformation("CreateOrderCommand succeeded - RequestId: {RequestId}", requestId);

        // ====== Lấy order mới tạo của đúng UserId trong body ======
        var orders = await services.Queries.GetOrdersFromUserAsync(request.UserId);
        var createdOrder = orders
            .OrderByDescending(o => o.OrderNumber)
            .FirstOrDefault();

        if (createdOrder == null)
        {
            services.Logger.LogWarning(
                "Order created but not found in queries. UserId={UserId}", request.UserId);

            return TypedResults.BadRequest("Order created but cannot be loaded.");
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
    List<BasketItem> Items);
