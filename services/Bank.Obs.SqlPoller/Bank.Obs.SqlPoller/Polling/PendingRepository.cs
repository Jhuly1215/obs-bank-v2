using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Bank.Obs.SqlPoller.Polling;

public sealed class PendingRepository
{
    private readonly ISqlExecutor _executor;

    public PendingRepository(ISqlExecutor executor)
    {
        _executor = executor;
    }

    public async Task<(
        int PendingCount24h, int PendingCount7d, int PendingOldestSec,
        IReadOnlyList<StateCountRow> OpPending, IReadOnlyList<StateCountRow> Programmed,
        ReviewStatsRow Review, IReadOnlyList<PendingBucketRow> PendingBucket,
        IReadOnlyList<AgeStatsRow> PendingAgeStats
    )> GetIntraMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        var count24h = await _executor.ScalarIntAsync(conn, SqlQueries.IntraPendingCount24h, ct);
        var count7d = await _executor.ScalarIntAsync(conn, SqlQueries.IntraPendingCount7d, ct);
        var oldestSec = await _executor.ScalarIntAsync(conn, SqlQueries.IntraPendingOldestSeconds, ct);
        
        var opPending = await _executor.QueryListAsync(conn, SqlQueries.IntraOpPendingCount, r => new StateCountRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1)), ct);
        var programmed = await _executor.QueryListAsync(conn, SqlQueries.IntraProgrammedCount, r => new StateCountRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1)), ct);
        var review = await _executor.QuerySingleAsync(conn, SqlQueries.IntraReviewStats, r => new ReviewStatsRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetInt(r, 2), SqlReaderHelper.GetInt(r, 3)), ct) ?? new ReviewStatsRow(0, 0, 0, 0);
        var bucket = await _executor.QueryListAsync(conn, SqlQueries.IntraPendingAgingBucketCount, r => new PendingBucketRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetInt(r, 2), SqlReaderHelper.GetInt(r, 3)), ct);
        var ageStats = await _executor.QueryListAsync(conn, SqlQueries.IntraPendingAgeStats, r => new AgeStatsRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetInt(r, 2)), ct);

        return (count24h, count7d, oldestSec, opPending, programmed, review, bucket, ageStats);
    }

    public async Task<(
        int PendingCount24h, int PendingCount7d, int PendingOldestSec,
        IReadOnlyList<StateCountRow> OpPending, IReadOnlyList<StateCountRow> Programmed,
        ReviewStatsRow Review, IReadOnlyList<PendingBucketRow> PendingBucket,
        IReadOnlyList<AgeStatsRow> PendingAgeStats
    )> GetInterMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        var count24h = await _executor.ScalarIntAsync(conn, SqlQueries.InterPendingCount24h, ct);
        var count7d = await _executor.ScalarIntAsync(conn, SqlQueries.InterPendingCount7d, ct);
        var oldestSec = await _executor.ScalarIntAsync(conn, SqlQueries.InterPendingOldestSeconds, ct);
        
        var opPending = await _executor.QueryListAsync(conn, SqlQueries.InterOpPendingCount, r => new StateCountRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1)), ct);
        var programmed = await _executor.QueryListAsync(conn, SqlQueries.InterProgrammedCount, r => new StateCountRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1)), ct);
        var review = await _executor.QuerySingleAsync(conn, SqlQueries.InterReviewStats, r => new ReviewStatsRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetInt(r, 2), SqlReaderHelper.GetInt(r, 3)), ct) ?? new ReviewStatsRow(0, 0, 0, 0);
        var bucket = await _executor.QueryListAsync(conn, SqlQueries.InterPendingAgingBucketCount, r => new PendingBucketRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetInt(r, 2), SqlReaderHelper.GetInt(r, 3)), ct);
        var ageStats = await _executor.QueryListAsync(conn, SqlQueries.InterPendingAgeStats, r => new AgeStatsRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetInt(r, 2)), ct);

        return (count24h, count7d, oldestSec, opPending, programmed, review, bucket, ageStats);
    }
}
