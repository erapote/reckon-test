using System.Threading.Channels;
using Reckon.PointOfSale.WebApi.Handlers;
using Reckon.PointOfSale.WebApi.Models;
using Reckon.PointOfSale.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Channel (bounded for back-pressure) ──────────────────────────────────────
var channel = Channel.CreateBounded<EventRecord>(new BoundedChannelOptions(1_000)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleReader = true,   // only EventProcessorService reads
    SingleWriter = false    // any request thread may write
});

builder.Services.AddSingleton(channel.Writer);
builder.Services.AddSingleton(channel.Reader);

// ── Storage ───────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();

// ── Handlers ─────────────────────────────────────────────────────────────────
// Registered as IEventHandler so EventProcessorService can inject IEnumerable<IEventHandler>
builder.Services.AddSingleton<IEventHandler, UserCreatedHandler>();
builder.Services.AddSingleton<IEventHandler, OrderPlacedHandler>();
// Fallback is registered separately so it is never matched by the dictionary lookup
builder.Services.AddSingleton<FallbackHandler>();

// ── Background processor ──────────────────────────────────────────────────────
builder.Services.AddHostedService<EventProcessorService>();

// ── Observability ─────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();          // Swagger / scalar at /openapi/v1.json

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi();
}

// ─────────────────────────────────────────────────────────────────────────────
// POST /events
// ─────────────────────────────────────────────────────────────────────────────
app.MapPost("/events", async (
    EventRequest req,
    IEventStore store,
    ChannelWriter<EventRecord> writer,
    ILogger<Program> logger) =>
{
    if (logger.IsEnabled(LogLevel.Information))
    {
        logger.LogInformation("Request received — EventType={EventType} Timestamp={Timestamp}",
            req.EventType, req.Timestamp);
    }

    // ── Validation ────────────────────────────────────────────────────────────
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(req.EventType))
    {
        errors[nameof(req.EventType)] = ["eventType is required."];
    }

    if (req.Timestamp is null)
    {
        errors[nameof(req.Timestamp)] = ["timestamp is required."];
    }
    else if (req.Timestamp.Value > DateTimeOffset.UtcNow.AddSeconds(5)) // 5-second clock-skew allowance
    {
        errors[nameof(req.Timestamp)] = ["timestamp must not be in the future."];
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    // ── Store ─────────────────────────────────────────────────────────────────
    var record = new EventRecord
    {
        Id = Guid.NewGuid(),
        EventType = req.EventType!,
        Payload = req.Payload,
        Timestamp = req.Timestamp!.Value
    };

    store.Add(record);

    // ── Enqueue for async processing ──────────────────────────────────────────
    await writer.WriteAsync(record);

    if (logger.IsEnabled(LogLevel.Information))
    {
        logger.LogInformation("Event accepted  — EventId={EventId} EventType={EventType}",
            record.Id, record.EventType);
    }

    return Results.Accepted($"/events/{record.Id}", record);
})
.WithName("PostEvent")
.WithSummary("Accept a POS event for asynchronous processing")
.Produces<EventRecord>(StatusCodes.Status202Accepted)
.ProducesValidationProblem();

// ─────────────────────────────────────────────────────────────────────────────
// GET /events
// ─────────────────────────────────────────────────────────────────────────────
app.MapGet("/events", (IEventStore store, ILogger<Program> logger) =>
{
    var events = store.GetAll();

    if (logger.IsEnabled(LogLevel.Information))
    {
        logger.LogInformation("GET /events — returning {Count} events", events.Count);
    }

    return Results.Ok(events);
})
.WithName("GetEvents")
.WithSummary("Return all stored events")
.Produces<IReadOnlyList<EventRecord>>();

// ─────────────────────────────────────────────────────────────────────────────
// GET /events/{id}
// ─────────────────────────────────────────────────────────────────────────────
app.MapGet("/events/{id:guid}", (Guid id, IEventStore store) =>
{
    var record = store.GetById(id);
    return record is null ? Results.NotFound() : Results.Ok(record);
})
.WithName("GetEventById")
.WithSummary("Return a single event by ID")
.Produces<EventRecord>()
.Produces(StatusCodes.Status404NotFound);

app.Run();

// Make Program visible to WebApplicationFactory in integration tests
internal partial class Program { }
