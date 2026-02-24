using System.Threading;
using System.Threading.Tasks;

namespace Bank.Obs.DemoApi.Services;

public interface ITransferService
{
    Task<TransferResult> SimulateTransferAsync(string channel, CancellationToken ct);
}

public sealed record TransferResult(string TransactionId, bool Success, string Outcome, string? ErrorCode);