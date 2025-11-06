// ﻿namespace eShop.Catalog.API.IntegrationEvents;

public sealed class CatalogIntegrationEventService(
    ILogger<CatalogIntegrationEventService> logger,
    CatalogContext catalogContext,
    IIntegrationEventLogService integrationEventLogService)
    : ICatalogIntegrationEventService, IDisposable
{
    private volatile bool disposedValue;

    public async Task PublishThroughEventBusAsync(IntegrationEvent evt)
    {
        try
        {
            // Demo mode: không có IEventBus, chỉ log + cập nhật IntegrationEventLog
            logger.LogInformation(
                "DEMO: Pretend publishing integration event: {IntegrationEventId} - ({@IntegrationEvent})",
                evt.Id, evt);

            await integrationEventLogService.MarkEventAsInProgressAsync(evt.Id);

            // KHÔNG gọi eventBus.PublishAsync(evt) vì không có RabbitMQ/EventBus
            // Giả lập publish thành công để không bị retry
            await integrationEventLogService.MarkEventAsPublishedAsync(evt.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error (DEMO) publishing integration event: {IntegrationEventId} - ({@IntegrationEvent})",
                evt.Id, evt);

            await integrationEventLogService.MarkEventAsFailedAsync(evt.Id);
        }
    }

    public async Task SaveEventAndCatalogContextChangesAsync(IntegrationEvent evt)
    {
        logger.LogInformation(
            "CatalogIntegrationEventService - Saving changes and integrationEvent: {IntegrationEventId}",
            evt.Id);

        // Dùng resilient transaction như cũ
        await ResilientTransaction.New(catalogContext).ExecuteAsync(async () =>
        {
            await catalogContext.SaveChangesAsync();
            await integrationEventLogService.SaveEventAsync(
                evt, catalogContext.Database.CurrentTransaction);
        });
    }

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                (integrationEventLogService as IDisposable)?.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
