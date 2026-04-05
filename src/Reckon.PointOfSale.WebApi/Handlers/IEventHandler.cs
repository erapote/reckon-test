using Reckon.PointOfSale.WebApi.Models;

namespace Reckon.PointOfSale.WebApi.Handlers;

/// <summary>
/// Contract that every event-type handler must implement.
/// </summary>
internal interface IEventHandler
{
    string EventType { get; }
    Task HandleAsync(EventRecord record, CancellationToken ct);
}
