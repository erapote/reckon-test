using System.Text.Json;
using Reckon.PointOfSale.WebApi.Models;

namespace Reckon.PointOfSale.WebApi.Handlers;

/// <summary>
/// Handles UserCreated events.
/// Real logic might persist a user record, send a welcome e-mail, etc.
/// </summary>
internal sealed partial class UserCreatedHandler(ILogger<UserCreatedHandler> logger) : IEventHandler
{
    public string EventType => "UserCreated";

    public async Task HandleAsync(EventRecord record, CancellationToken ct)
    {
        LogProcessingEvent(record.Id, record.Payload);

        // Simulate I/O-bound work (e.g. sending a welcome e-mail)
        await Task.Delay(50, ct);

        LogUserProvisioned(record.Id);
    }

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "[UserCreated] Processing event {EventId}. Payload: {Payload}")]
    private partial void LogProcessingEvent(Guid eventId, JsonElement? payload);

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "[UserCreated] User provisioned for event {EventId}")]
    private partial void LogUserProvisioned(Guid eventId);
}
