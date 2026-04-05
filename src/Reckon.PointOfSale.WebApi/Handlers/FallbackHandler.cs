using Reckon.PointOfSale.WebApi.Models;

namespace Reckon.PointOfSale.WebApi.Handlers;

/// <summary>
/// Catch-all handler for event types that have no dedicated handler registered.
/// Logs a warning rather than silently dropping the event.
/// </summary>
internal sealed class FallbackHandler(ILogger<FallbackHandler> logger) : IEventHandler
{
    public string EventType => "*";

    public Task HandleAsync(EventRecord record, CancellationToken ct)
    {
        logger.LogWarning(
            "[Fallback] No handler registered for event type '{EventType}' (event {EventId}). Event stored but not processed.",
            record.EventType,
            record.Id);

        return Task.CompletedTask;
    }
}
