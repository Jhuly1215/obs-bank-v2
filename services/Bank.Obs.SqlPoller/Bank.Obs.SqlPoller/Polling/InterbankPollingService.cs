using Microsoft.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace Bank.Obs.SqlPoller.Polling;

public sealed class InterbankPollingService
{
    private readonly HistoricalMetricsRepository _historical;
    private readonly PendingRepository _pending;
    private readonly ResolutionRepository _resolution;
    private readonly FailureRepository _failure;
    private readonly DistributionRepository _distribution;
    private readonly AmountRepository _amount;
    private readonly InterbankRepository _interbank;

    public InterbankPollingService(
        HistoricalMetricsRepository historical,
        PendingRepository pending,
        ResolutionRepository resolution,
        FailureRepository failure,
        DistributionRepository distribution,
        AmountRepository amount,
        InterbankRepository interbank)
    {
        _historical = historical;
        _pending = pending;
        _resolution = resolution;
        _failure = failure;
        _distribution = distribution;
        _amount = amount;
        _interbank = interbank;
    }

    public async Task<InterbankSnapshot> PollAsync(string connString, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connString);
        await conn.OpenAsync(ct);

        var hist = await _historical.GetInterMetricsAsync(conn, ct);
        var pend = await _pending.GetInterMetricsAsync(conn, ct);
        var res = await _resolution.GetInterMetricsAsync(conn, ct);
        var fail = await _failure.GetInterMetricsAsync(conn, ct);
        var dist = await _distribution.GetInterMetricsAsync(conn, ct);
        var amt = await _amount.GetInterMetricsAsync(conn, ct);
        var ibank = await _interbank.GetMetricsAsync(conn, ct);

        return new InterbankSnapshot(
            hist.TxCreated5m, hist.TxCreated15m, hist.TxCreated1h, hist.TxCreated24h, hist.TxCreated7d, hist.TxCreated30d,
            pend.PendingCount24h, pend.PendingCount7d, pend.PendingOldestSec,
            fail.ErrorCount24h, res.ResolvedCount24h, res.ResAvgSec,
            dist.StateCount24h, pend.OpPending, pend.Programmed,
            pend.Review, pend.PendingBucket, fail.Failures,
            dist.TypeCount, amt.AmountTotal, dist.AmountByType,
            res.SuccessSpeed,
            ibank.InterBankCount, ibank.InterBankStateCount, ibank.InterBankAmountTotal,
            res.SuccessCount24h, pend.PendingAgeStats, amt.AmountTotal1h, res.SuccessSpeedP99,
            fail.ZeroDurationCount, fail.MissingModCount, res.ClosedCount24h, dist.OtherStateCount24h, fail.CompensatedCurrentCount
        );
    }
}
