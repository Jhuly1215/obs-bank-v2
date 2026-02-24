using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bank.Obs.DemoApi.Services;

public sealed class TransferService : ITransferService
{
    private readonly ILogger<TransferService> _logger;
    private readonly TransferSimulatorOptions _opt;

    public TransferService(ILogger<TransferService> logger, IOptions<TransferSimulatorOptions> opt)
    {
        _logger = logger;
        _opt = opt.Value;
    }

    public async Task<TransferResult> SimulateTransferAsync(string channel, CancellationToken ct)
    {
        var transactionId = Guid.NewGuid().ToString("N");

        _logger.LogInformation(
            "Transfer started transaction_id={transaction_id} channel={channel} outcome={outcome}",
            transactionId, channel, "pending");

        var delay = Random.Shared.Next(_opt.MinLatencyMs, _opt.MaxLatencyMs + 1);
        await Task.Delay(delay, ct);

        var success = Random.Shared.NextDouble() > _opt.FailureRate;
        if (!success)
        {
            _logger.LogError(
                "Transfer failed transaction_id={transaction_id} error_code={error_code} outcome={outcome}",
                transactionId, _opt.ErrorCode, "failed");

            return new TransferResult(transactionId, false, "failed", _opt.ErrorCode);
        }

        _logger.LogInformation(
            "Transfer completed transaction_id={transaction_id} outcome={outcome}",
            transactionId, "success");

        return new TransferResult(transactionId, true, "success", null);
    }
}