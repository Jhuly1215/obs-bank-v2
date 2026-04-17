using StackExchange.Redis;
using System.Text.Json;

namespace Bank.Obs.DemoApi.Services;

public sealed class RedisIdempotencyStore : IIdempotencyStore
{
    private readonly IConnectionMultiplexer _redis;
    private IDatabase _db => _redis.GetDatabase();

    public RedisIdempotencyStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<IdempotencyRecord?> GetAsync(string key, CancellationToken ct)
    {
        var value = await _db.StringGetAsync(key);
        if (!value.HasValue) return null;

        // .NET 10 Fix: Explicitly cast RedisValue to string to resolve ambiguity
        // between string and ReadOnlySpan<byte> overloads in JsonSerializer.
        return JsonSerializer.Deserialize<IdempotencyRecord>((string)value!);
    }

    public async Task<bool> TryReserveAsync(string key, TimeSpan ttl, CancellationToken ct)
    {
        return await _db.StringSetAsync(key, "RESERVED", expiry: ttl, when: When.NotExists);
    }

    public async Task SetAsync(string key, IdempotencyRecord record, TimeSpan ttl, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(record);
        await _db.StringSetAsync(key, payload, expiry: ttl, when: When.Always);
    }
}
