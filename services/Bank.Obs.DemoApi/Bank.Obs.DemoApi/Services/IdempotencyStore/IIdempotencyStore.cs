using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bank.Obs.DemoApi.Services;

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> GetAsync(string key, CancellationToken ct);
    Task<bool> TryReserveAsync(string key, TimeSpan ttl, CancellationToken ct);
    Task SetAsync(string key, IdempotencyRecord record, TimeSpan ttl, CancellationToken ct);
}