using Reckon.PointOfSale.WebApi.Models;

namespace Reckon.PointOfSale.WebApi.Handlers;

/// <summary>
/// Handles OrderPlaced events.
/// Real logic might update inventory, trigger fulfilment, charge a card, etc.
/// </summary>
internal sealed partial class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger) : IEventHandler
{
    public string EventType => "OrderPlaced";

    public async Task HandleAsync(EventRecord record, CancellationToken ct)
    {
        var payload = record.Payload.ToString();
        LogOrderInitialized(record.Id, payload);

        // Simulate heavier I/O (inventory check, payment, fulfilment kick-off)
        await Task.Delay(120, ct);

        // Extract a simple field from the free-form payload for demo purposes
        if (record.Payload.TryGetProperty("orderId", out var orderIdEl))
        {
            var orderId = orderIdEl.ToString();
            LogOrderInfo(orderId, record.Id);
        }
        else
        {
            LogOrderQueued(record.Id);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "[OrderPlaced] Processing event {EventId}. Payload: {Payload}")]
    private partial void LogOrderInitialized(Guid eventId, string payload);

    [LoggerMessage(Level = LogLevel.Information, Message = "[OrderPlaced] Order {OrderId} queued for fulfilment (event {EventId})")]
    private partial void LogOrderInfo(string orderId, Guid eventId);

    [LoggerMessage(Level = LogLevel.Information, Message = "[OrderPlaced] Order queued for fulfilment (event {EventId})")]
    private partial void LogOrderQueued(Guid eventId);

}
