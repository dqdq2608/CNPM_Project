using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using eShop.EventBus.Abstractions;
using eShop.EventBus.Extensions;
using Microsoft.Extensions.DependencyInjection;  // Đảm bảo namespace này được thêm
using Scrutor; // Thêm Scrutor

namespace Microsoft.Extensions.DependencyInjection;

public static class EventBusBuilderExtensions
{
    public static IEventBusBuilder ConfigureJsonOptions(this IEventBusBuilder eventBusBuilder, Action<JsonSerializerOptions> configure)
    {
        eventBusBuilder.Services.Configure<EventBusSubscriptionInfo>(o =>
        {
            configure(o.JsonSerializerOptions);
        });

        return eventBusBuilder;
    }

    public static IEventBusBuilder AddSubscription<T, TH>(this IEventBusBuilder eventBusBuilder)
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>
    {
        // Đăng ký các handler sự kiện với Scrutor
        eventBusBuilder.Services.Scan(scan => scan
            .FromAssemblyOf<IIntegrationEventHandler<T>>()
            .AddClasses(classes => classes.AssignableTo<IIntegrationEventHandler<T>>())
            .AsImplementedInterfaces()
            .WithTransientLifetime() // Đăng ký các lớp này với Transient lifetime
        );

        // Đăng ký keyed service
        eventBusBuilder.Services.AddTransient<IIntegrationEventHandler<T>, TH>();

        eventBusBuilder.Services.Configure<EventBusSubscriptionInfo>(o =>
        {
            o.EventTypes[typeof(T).Name] = typeof(T);
        });

        return eventBusBuilder;
    }
}
