using eShop.Catalog.API.Services;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace eShop.Catalog.API.Extensions;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        // DbContext + vector type (pgvector)
        builder.AddNpgsqlDbContext<CatalogContext>(
            "catalogdb",
            configureDbContextOptions: options =>
            {
                options.UseNpgsql(npgsql =>
               {
                   // pgvector
                   npgsql.UseVector();
                   // 🔸 Bật PostGIS/NetTopologySuite
                   npgsql.UseNetTopologySuite();
               });
            });

        // Migration seeding
        builder.Services.AddMigration<CatalogContext, CatalogContextSeed>();

        // Integration event log services (không phụ thuộc RabbitMQ)
        builder.Services.AddTransient<IIntegrationEventLogService, IntegrationEventLogService<CatalogContext>>();
        builder.Services.AddTransient<ICatalogIntegrationEventService, CatalogIntegrationEventService>();

        // TODO (đã tắt cho demo): Event Bus RabbitMQ
        // builder.AddRabbitMqEventBus("eventbus")
        //        .AddSubscription<OrderStatusChangedToAwaitingValidationIntegrationEvent, OrderStatusChangedToAwaitingValidationIntegrationEventHandler>()
        //        .AddSubscription<OrderStatusChangedToPaidIntegrationEvent, OrderStatusChangedToPaidIntegrationEventHandler>();

        // Options
        builder.Services.AddOptions<CatalogOptions>()
               .BindConfiguration(nameof(CatalogOptions));

        // TODO (đã tắt cho demo): ONNX / Azure OpenAI embeddings
        // if (builder.Configuration["AI:Onnx:EmbeddingModelPath"] is string modelPath &&
        //     builder.Configuration["AI:Onnx:EmbeddingVocabPath"] is string vocabPath)
        // {
        //     builder.Services.AddBertOnnxTextEmbeddingGeneration(modelPath, vocabPath);
        // }
        // else if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("openai")))
        // {
        //     builder.AddAzureOpenAIClient("openai");
        //     builder.Services.AddOpenAITextEmbeddingGeneration(
        //         builder.Configuration["AIOptions:OpenAI:EmbeddingName"] ?? "text-embedding-3-small");
        // }

        // AI service vẫn đăng ký, nhưng không có embedding generator => IsEnabled=false (an toàn cho demo)
        builder.Services.AddSingleton<ICatalogAI, CatalogAI>();
    }
}
