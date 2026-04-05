using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reckon.PointOfSale.WebApi.Models;

/// <summary>
/// Incoming JSON payload from the client.
/// </summary>
internal sealed record EventRequest
{
    [JsonPropertyName("eventType")]
    public required string EventType { get; init; }

    [JsonPropertyName("payload")]
    public required JsonElement Payload { get; init; }

    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }
}
