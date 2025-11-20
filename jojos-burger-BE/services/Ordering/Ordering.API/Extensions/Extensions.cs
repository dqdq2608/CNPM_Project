using eShop.EventBus;
using eShop.EventBus.Abstractions;
using eShop.Ordering.API.Application.IntegrationEvents.Events;
using eShop.Ordering.API.IntegrationEvents.EventHandling;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// alias 2 event thanh toán để tránh trùng tên với event cũ trong Ordering
using PaymentFailedIntegrationEvent = Payment.IntegrationEvents.Events.OrderPaymentFailedIntegrationEvent;
using PaymentSucceededIntegrationEvent = Payment.IntegrationEvents.Events.OrderPaymentSucceededIntegrationEvent;

internal static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        // DbContext
        services.AddDbContext<OrderingContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("orderingdb"));
        });

        // Integration event log + ordering integration service
        services.AddTransient<IIntegrationEventLogService, IntegrationEventLogService<OrderingContext>>();
        services.AddTransient<IOrderingIntegrationEventService, OrderingIntegrationEventService>();

        services.AddHttpContextAccessor();
        services.AddTransient<IIdentityService, IdentityService>();

        // MediatR + behaviors
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining(typeof(Program));

            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidatorBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        // FluentValidation validators
        services.AddValidatorsFromAssemblyContaining<CancelOrderCommandValidator>();

        // Repositories, queries
        services.AddScoped<IOrderQueries, OrderQueries>();
        services.AddScoped<IBuyerRepository, BuyerRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IRequestManager, RequestManager>();

        // ĐĂNG KÝ DI cho 2 handler payment (rất quan trọng)
        services.AddTransient<OrderPaymentFailedIntegrationEventHandler>();
        services.AddTransient<OrderPaymentSucceededIntegrationEventHandler>();
    }

    /// <summary>
    /// Đăng ký các subscription cho EventBus (Ordering lắng nghe các event bên ngoài).
    /// </summary>
    private static void AddEventBusSubscriptions(this IEventBusBuilder eventBus)
    {
        // Các event cũ của Ordering
        eventBus.AddSubscription<GracePeriodConfirmedIntegrationEvent, GracePeriodConfirmedIntegrationEventHandler>();
        eventBus.AddSubscription<OrderStockConfirmedIntegrationEvent, OrderStockConfirmedIntegrationEventHandler>();
        eventBus.AddSubscription<OrderStockRejectedIntegrationEvent, OrderStockRejectedIntegrationEventHandler>();

        // 🔥 Các event thanh toán dùng TYPE từ Payment.IntegrationEvents.Events
        eventBus.AddSubscription<PaymentFailedIntegrationEvent, OrderPaymentFailedIntegrationEventHandler>();
        eventBus.AddSubscription<PaymentSucceededIntegrationEvent, OrderPaymentSucceededIntegrationEventHandler>();
    }
}
