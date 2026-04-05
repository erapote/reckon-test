using System.Collections.Concurrent;
using Reckon.PointOfSale.WebApi.Models;

namespace Reckon.PointOfSale.WebApi.Services;

internal interface IEventStore
{
    void Add(EventRecord @event);
    IReadOnlyList<EventRecord> GetAll();
    EventRecord? GetById(Guid id);
}

/// <summary>
/// Thread-safe in-memory store backed by a ConcurrentDictionary.
/// Insertion order is preserved via a separate list guarded by a lock.
/// </summary>
internal sealed class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentDictionary<Guid, EventRecord> _store = new();
    private readonly List<Guid> _insertionOrder = [];
    private readonly Lock _orderLock = new();

    public void Add(EventRecord @event)
    {
        _store[@event.Id] = @event;
        lock (_orderLock)
        {
            _insertionOrder.Add(@event.Id);
        }
    }

    public IReadOnlyList<EventRecord> GetAll()
    {
        lock (_orderLock)
        {
            return _insertionOrder
                .Select(id => _store[id])
                .ToList()
                .AsReadOnly();
        }
    }

    public EventRecord? GetById(Guid id) =>
        _store.TryGetValue(id, out var record) ? record : null;
}
