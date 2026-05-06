using Microsoft.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace Bank.Obs.SqlPoller.Polling;

public sealed class IntraPollingService
{
    private readonly HistoricalMetricsRepository _historical;
    private readonly PendingRepository _pending;
    private readonly ResolutionRepository _resolution;
    private readonly FailureRepository _failure;
    private readonly DistributionRepository _distribution;
    private readonly AmountRepository _amount;

    public IntraPollingService(
        HistoricalMetricsRepository historical,
        PendingRepository pending,
        ResolutionRepository resolution,
        FailureRepository failure,
        DistributionRepository distribution,
        AmountRepository amount)
    {
        _historical = historical;
        _pending = pending;
        _resolution = resolution;
        _failure = failure;
        _distribution = distribution;
        _amount = amount;
    }

    public async Task<IntraSnapshot> PollAsync(string connString, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connString);
        await conn.OpenAsync(ct);

        var hist = await _historical.GetIntraMetricsAsync(conn, ct);
        var pend = await _pending.GetIntraMetricsAsync(conn, ct);
        var res = await _resolution.GetIntraMetricsAsync(conn, ct);
        var fail = await _failure.GetIntraMetricsAsync(conn, ct);
        var dist = await _distribution.GetIntraMetricsAsync(conn, ct);
        var amt = await _amount.GetIntraMetricsAsync(conn, ct);

        return new IntraSnapshot(
            hist.TxCreated5m, hist.TxCreated15m, hist.TxCreated1h, hist.TxCreated24h, hist.TxCreated7d, hist.TxCreated30d,
            pend.PendingCount24h, pend.PendingCount7d, pend.PendingOldestSec,
            fail.ErrorCount24h, res.ResolvedCount24h, res.ResAvgSec,
            dist.StateCount24h, pend.OpPending, pend.Programmed,
            pend.Review, pend.PendingBucket, fail.Failures,
            dist.TypeCount, amt.AmountTotal, dist.AmountByType,
            res.SuccessSpeed,
            res.SuccessCount24h, pend.PendingAgeStats, amt.AmountTotal1h, res.SuccessSpeedP99,
            fail.ZeroDurationCount, fail.MissingModCount, res.ClosedCount24h, dist.OtherStateCount24h, fail.CompensatedCurrentCount
        );
    }
}
