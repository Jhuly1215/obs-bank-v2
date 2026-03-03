using System.Collections.Concurrent;

namespace Bank.Obs.DemoApi.Services;

public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, IdempotencyRecord> _store = new();

    public Task<IdempotencyRecord?> GetAsync(string key, CancellationToken ct)
        => Task.FromResult(_store.TryGetValue(key, out var record) ? record : null);

    public Task<bool> TryReserveAsync(string key, TimeSpan ttl, CancellationToken ct)
    {
        // Reserva "pending" si no existe (atomiza en memoria)
        var pending = new IdempotencyRecord(
            Key: key,
            TransactionId: "",
            Status: "pending",
            ResponsePayload: "",
            CreatedAt: DateTime.UtcNow
        );

        var ok = _store.TryAdd(key, pending);
        return Task.FromResult(ok);
    }

    public Task SetAsync(string key, IdempotencyRecord record, TimeSpan ttl, CancellationToken ct)
    {
        _store[key] = record;
        return Task.CompletedTask;
    }
}