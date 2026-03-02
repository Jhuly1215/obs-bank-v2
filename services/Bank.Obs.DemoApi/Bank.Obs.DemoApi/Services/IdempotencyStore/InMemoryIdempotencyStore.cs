using System;
using System.Collections.Concurrent;

namespace Bank.Obs.DemoApi.Services;

public sealed record IdempotencyRecord(
    string Key,
    string TransactionId,
    string Status,
    string ResponsePayload,
    DateTime CreatedAt);

public sealed class IdempotencyStore
{
    private readonly ConcurrentDictionary<string, IdempotencyRecord> _store = new();

    public bool TryGet(string key, out IdempotencyRecord? record) => _store.TryGetValue(key, out record);

    public void Save(string key, IdempotencyRecord record) => _store[key] = record;
}