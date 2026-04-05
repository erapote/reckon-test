using System.Text.Json;

namespace Reckon.PointOfSale.WebApi.Models;

/// <summary>
/// Stored representation of an event, enriched with an internal Id and status.
/// </summary>
internal sealed record EventRecord
{
    public Guid Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public JsonElement Payload { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public EventStatus Status { get; set; } = EventStatus.Pending;
    public DateTimeOffset ReceivedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? ErrorMessage { get; set; }
}
