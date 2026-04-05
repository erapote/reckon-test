using System.Diagnostics;
using System.Threading.Channels;
using Reckon.PointOfSale.WebApi.Handlers;
using Reckon.PointOfSale.WebApi.Models;

namespace Reckon.PointOfSale.WebApi.Services;

/// <summary>
/// Hosted background service that drains the event channel and dispatches
/// each event to its registered handler.
/// </summary>
internal sealed partial class EventProcessorService(
    ChannelReader<EventRecord> reader,
    IEnumerable<IEventHandler> handlers,
    FallbackHandler fallback,
    ILogger<EventProcessorService> logger) : BackgroundService
{
    // Named ActivitySource for distributed tracing (OpenTelemetry-compatible)
    public static readonly ActivitySource ActivitySource = new("PosEvents.Processor");

    private readonly Dictionary<string, IEventHandler> _handlers =
        handlers.ToDictionary(h => h.EventType, h => h, StringComparer.OrdinalIgnoreCase);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EventProcessorService started, waiting for events…");

        await foreach (var record in reader.ReadAllAsync(stoppingToken))
        {
            await ProcessAsync(record, stoppingToken);
        }

        logger.LogInformation("EventProcessorService stopped.");
    }

    private async Task ProcessAsync(EventRecord record, CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity("ProcessEvent");
        _ = activity?.SetTag("event.id", record.Id.ToString());
        _ = activity?.SetTag("event.type", record.EventType);

        LogProcessingStarted(logger, record.Id, record.EventType);

        record.Status = EventStatus.Processing;

        var handler = _handlers.TryGetValue(record.EventType, out var h) ? h : fallback;

        try
        {
            await handler.HandleAsync(record, ct);

            record.Status = EventStatus.Completed;

            _ = activity?.SetTag("event.status", "completed");

            LogProcessingCompleted(logger, record.Id, record.EventType);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            record.Status = EventStatus.Failed;
            record.ErrorMessage = ex.Message;
            _ = activity?.SetTag("event.status", "failed");
            _ = activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            LogProcessingFailed(logger, record.Id, record.EventType, ex);
        }
    }

    [LoggerMessage(LogLevel.Information, "Processing started  — EventId={eventId} EventType={eventType}")]
    private static partial void LogProcessingStarted(ILogger logger, Guid eventId, string eventType);

    [LoggerMessage(LogLevel.Information, "Processing completed — EventId={eventId} EventType={eventType}")]
    private static partial void LogProcessingCompleted(ILogger logger, Guid eventId, string eventType);

    [LoggerMessage(LogLevel.Error, "Processing failed    — EventId={eventId} EventType={eventType}")]
    private static partial void LogProcessingFailed(ILogger logger, Guid eventId, string eventType, Exception ex);
}
