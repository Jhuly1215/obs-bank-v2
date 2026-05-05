using Microsoft.Data.SqlClient;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace Bank.Obs.SqlPoller.Polling;

public sealed class SnapshotPollingService
{
    private readonly ISqlExecutor _executor;
    private readonly HistoricalMetricsRepository _historical;
    private readonly PendingRepository _pending;
    private readonly ResolutionRepository _resolution;
    private readonly FailureRepository _failure;
    private readonly DistributionRepository _distribution;
    private readonly AmountRepository _amount;
    private readonly InterbankRepository _interbank;

    public SnapshotPollingService(
        ISqlExecutor executor,
        HistoricalMetricsRepository historical,
        PendingRepository pending,
        ResolutionRepository resolution,
        FailureRepository failure,
        DistributionRepository distribution,
        AmountRepository amount,
        InterbankRepository interbank)
    {
        _executor = executor;
        _historical = historical;
        _pending = pending;
        _resolution = resolution;
        _failure = failure;
        _distribution = distribution;
        _amount = amount;
        _interbank = interbank;
    }

    public async Task<Snapshot> PollAsync(string connString, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connString);
        await conn.OpenAsync(ct);

        // 0) Context
        var dayTypeObj = await new SqlCommand(SqlQueries.GetDayType, conn).ExecuteScalarAsync(ct);
        var dayType = dayTypeObj?.ToString() ?? "habil";

        // 1) Históricos
        var hist = await _historical.GetMetricsAsync(conn, ct);
        
        // 2) Pendientes
        var pend = await _pending.GetMetricsAsync(conn, ct);

        // 3) Resolución
        var res = await _resolution.GetMetricsAsync(conn, ct);

        // 4) Fallas y Anomalías
        var fail = await _failure.GetMetricsAsync(conn, ct);

        // 5) Distribución
        var dist = await _distribution.GetMetricsAsync(conn, ct);

        // 6) Montos
        var amt = await _amount.GetMetricsAsync(conn, ct);

        // 7) Interbancario
        var interbank = await _interbank.GetMetricsAsync(conn, ct);

        // Health & Resources (Comentados)
        var serverSessions = Array.Empty<SessionStatRow>();
        var fileIoStats = Array.Empty<IoStatRow>();
        var databaseSizes = Array.Empty<DatabaseSizeRow>();

        return new Snapshot(
            dayType,

            hist.IntraTxCreated15m, hist.InterTxCreated15m,
            hist.IntraTxCreated24h, hist.InterTxCreated24h,
            hist.IntraTxCreated7d, hist.InterTxCreated7d,
            hist.IntraTxCreated30d, hist.InterTxCreated30d,
            pend.IntraPendingCount24h, pend.InterPendingCount24h,
            pend.IntraPendingCount7d, pend.InterPendingCount7d,
            pend.IntraPendingOldestSec, pend.InterPendingOldestSec,
            fail.IntraErrorCount24h, fail.InterErrorCount24h,
            res.IntraResolvedCount24h, res.InterResolvedCount24h,
            res.IntraResAvgSec, res.InterResAvgSec,

            dist.IntraStateCount24h, dist.InterStateCount24h,
            pend.IntraOpPending, pend.InterOpPending,
            pend.IntraProgrammed, pend.InterProgrammed,
            pend.IntraReview, pend.InterReview,
            pend.IntraPendingBucket, pend.InterPendingBucket,
            fail.IntraFailures, fail.InterFailures,
            dist.IntraTypeCount, dist.InterTypeCount,
            amt.IntraAmountTotal, amt.InterAmountTotal, 
            dist.IntraAmountByType, dist.InterAmountByType,
            res.IntraSuccessSpeed, res.InterSuccessSpeed,
            
            interbank.InterBankCount, interbank.InterBankStateCount, interbank.InterBankAmountTotal,

            serverSessions,
            fileIoStats,
            databaseSizes,
            fail.QualityAnomalies,

            res.IntraSuccessCount24h, res.InterSuccessCount24h,
            pend.IntraPendingAgeStats, pend.InterPendingAgeStats,
            hist.IntraTxCreated5m, hist.InterTxCreated5m,
            hist.IntraTxCreated1h, hist.InterTxCreated1h,
            amt.IntraAmountTotal1h, amt.InterAmountTotal1h,
            res.IntraSuccessSpeedP99, res.InterSuccessSpeedP99,
            fail.IntraZeroDurationCount, fail.InterZeroDurationCount,
            fail.IntraMissingModCount, fail.InterMissingModCount,
            res.IntraClosedCount24h, res.InterClosedCount24h,
            dist.IntraOtherStateCount24h, dist.InterOtherStateCount24h,
            fail.IntraCompensatedCurrentCount, fail.InterCompensatedCurrentCount
        );
    }
}
