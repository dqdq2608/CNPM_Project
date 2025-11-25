namespace eShop.Ordering.API.Application.Commands;

using eShop.Ordering.API.Application.IntegrationEvents.Events;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;

public class CreateOrderCommandHandler
    : IRequestHandler<CreateOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IIdentityService _identityService;
    private readonly IMediator _mediator;
    private readonly IOrderingIntegrationEventService _orderingIntegrationEventService;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IMediator mediator,
        IOrderingIntegrationEventService orderingIntegrationEventService,
        IOrderRepository orderRepository,
        IIdentityService identityService,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _orderingIntegrationEventService = orderingIntegrationEventService ?? throw new ArgumentNullException(nameof(orderingIntegrationEventService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> Handle(CreateOrderCommand message, CancellationToken cancellationToken)
    {
        // =============================================================
        // 1) PUBLISH OrderStartedIntegrationEvent (xoá basket)
        // =============================================================
        var orderStarted = new OrderStartedIntegrationEvent(message.UserId);
        await _orderingIntegrationEventService.AddAndSaveEventAsync(orderStarted);

        // Add/Update the Buyer AggregateRoot
        // DDD patterns comment: Add child entities and value-objects through the Order Aggregate-Root
        // methods and constructor so validations, invariants and business logic 
        // make sure that consistency is preserved across the whole aggregate
        var address = new Address(message.Street, message.City, message.State, message.Country, message.ZipCode);
        var order = new Order(message.UserId, message.UserName, address, message.DeliveryFee, message.RestuantId, message.CardTypeId, message.CardNumber, message.CardSecurityNumber, message.CardHolderName, message.CardExpiration);

        foreach (var item in message.OrderItems)
        {
            order.AddOrderItem(
                productId: item.ProductId,
                productName: item.ProductName,
                unitPrice: item.UnitPrice,
                discount: item.Discount,
                pictureUrl: item.PictureUrl,
                units: item.Units);
        }

        _logger.LogInformation("Creating Order {@Order}", order);

        _orderRepository.Add(order);

        // =============================================================
        // 3) SAVE LẦN 1  → sẽ phát ra:
        //      - OrderStartedIntegrationEvent
        //      - OrderStatusChangedToSubmittedIntegrationEvent
        // =============================================================
        var result = await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        if (!result)
            return false;

        _logger.LogInformation(
            ">>> [ORDERING] Order created with Id={OrderId} for UserId={UserId}",
            order.Id,
            message.UserId);

        // =============================================================
        // 4) AUTO CONFIRM STOCK → BẮN EVENT Payment
        // =============================================================
        //    This will raise:
        //      - OrderStatusChangedToStockConfirmedDomainEvent
        // =============================================================
        order.ForceSetStockConfirmedStatus();

        // đảm bảo EF track thay đổi
        _orderRepository.Update(order);

        // =============================================================
        // 5) SAVE LẦN 2  → sẽ phát ra:
        //      - OrderStatusChangedToStockConfirmedIntegrationEvent
        // =============================================================
        await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);


        return true;
    }
}


// =============================================================
//  Idempotency Handler
// =============================================================
public class CreateOrderIdentifiedCommandHandler
    : IdentifiedCommandHandler<CreateOrderCommand, bool>
{
    public CreateOrderIdentifiedCommandHandler(
        IMediator mediator,
        IRequestManager requestManager,
        ILogger<IdentifiedCommandHandler<CreateOrderCommand, bool>> logger)
        : base(mediator, requestManager, logger)
    {
    }

    protected override bool CreateResultForDuplicateRequest()
        => true;
}
