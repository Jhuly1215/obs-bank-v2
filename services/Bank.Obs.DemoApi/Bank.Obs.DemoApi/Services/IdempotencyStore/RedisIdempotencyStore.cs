using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Bank.Obs.DemoApi.Services;

public sealed class RedisIdempotencyStore : IIdempotencyStore
{
    private readonly IDatabase _db;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public RedisIdempotencyStore(IConnectionMultiplexer mux)
    {
        _db = mux.GetDatabase();
    }

    private static string K(string key) => $"idemp:{key}";

    public async Task<IdempotencyRecord?> GetAsync(string key, CancellationToken ct)
    {
        var value = await _db.StringGetAsync(K(key));
        if (!value.HasValue) return null;

        return JsonSerializer.Deserialize<IdempotencyRecord>(value!, _json);
    }

    public async Task<bool> TryReserveAsync(string key, TimeSpan ttl, CancellationToken ct)
    {
        // SET NX EX => reserva atómica (evita duplicados concurrentes)
        var pending = new IdempotencyRecord(
            Key: key,
            TransactionId: "",
            Status: "pending",
            ResponsePayload: "",
            CreatedAt: DateTime.UtcNow
        );

        var payload = JsonSerializer.Serialize(pending, _json);

        return await _db.StringSetAsync(
            K(key),
            payload,
            expiry: ttl,
            when: When.NotExists
        );
    }

    public async Task SetAsync(string key, IdempotencyRecord record, TimeSpan ttl, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(record, _json);

        await _db.StringSetAsync(
            K(key),
            payload,
            expiry: ttl,
            when: When.Always
        );
    }
}