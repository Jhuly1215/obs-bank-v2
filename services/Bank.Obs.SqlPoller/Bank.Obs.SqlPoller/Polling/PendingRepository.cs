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
        int IntraPendingCount24h, int InterPendingCount24h,
        int IntraPendingCount7d, int InterPendingCount7d,
        int IntraPendingOldestSec, int InterPendingOldestSec,
        IReadOnlyList<StateCountRow> IntraOpPending, IReadOnlyList<StateCountRow> InterOpPending,
        IReadOnlyList<StateCountRow> IntraProgrammed, IReadOnlyList<StateCountRow> InterProgrammed,
        ReviewStatsRow IntraReview, ReviewStatsRow InterReview,
        IReadOnlyList<PendingBucketRow> IntraPendingBucket, IReadOnlyList<PendingBucketRow> InterPendingBucket,
        IReadOnlyList<AgeStatsRow> IntraPendingAgeStats, IReadOnlyList<AgeStatsRow> InterPendingAgeStats
    )> GetMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        var intraPendingCount24h = await _executor.ScalarIntAsync(conn, SqlQueries.IntraPendingCount24h, ct);
        var interPendingCount24h = await _executor.ScalarIntAsync(conn, SqlQueries.InterPendingCount24h, ct);
        var intraPendingCount7d = await _executor.ScalarIntAsync(conn, SqlQueries.IntraPendingCount7d, ct);
        var interPendingCount7d = await _executor.ScalarIntAsync(conn, SqlQueries.InterPendingCount7d, ct);
        
        var intraPendingOldestSec = await _executor.ScalarIntAsync(conn, SqlQueries.IntraPendingOldestSeconds, ct);
        var interPendingOldestSec = await _executor.ScalarIntAsync(conn, SqlQueries.InterPendingOldestSeconds, ct);

        var intraOpPending = await _executor.QueryListAsync(conn, SqlQueries.IntraOpPendingCount, 
            r => new StateCountRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1)), ct);
        var interOpPending = await _executor.QueryListAsync(conn, SqlQueries.InterOpPendingCount, 
            r => new StateCountRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1)), ct);
        
        var intraProgrammed = await _executor.QueryListAsync(conn, SqlQueries.IntraProgrammedCount, 
            r => new StateCountRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1)), ct);
        var interProgrammed = await _executor.QueryListAsync(conn, SqlQueries.InterProgrammedCount, 
            r => new StateCountRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1)), ct);

        var intraReview = await _executor.QuerySingleAsync(conn, SqlQueries.IntraReviewStats, 
            r => new ReviewStatsRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetInt(r, 2), SqlReaderHelper.GetInt(r, 3)), ct) 
            ?? new ReviewStatsRow(0, 0, 0, 0);
        var interReview = await _executor.QuerySingleAsync(conn, SqlQueries.InterReviewStats, 
            r => new ReviewStatsRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetInt(r, 2), SqlReaderHelper.GetInt(r, 3)), ct)
            ?? new ReviewStatsRow(0, 0, 0, 0);

        var intraPendingBucket = await _executor.QueryListAsync(conn, SqlQueries.IntraPendingAgingBucketCount, 
            r => new PendingBucketRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetInt(r, 2), SqlReaderHelper.GetInt(r, 3)), ct);
        var interPendingBucket = await _executor.QueryListAsync(conn, SqlQueries.InterPendingAgingBucketCount, 
            r => new PendingBucketRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetInt(r, 2), SqlReaderHelper.GetInt(r, 3)), ct);

        var intraPendingAgeStats = await _executor.QueryListAsync(conn, SqlQueries.IntraPendingAgeStats, 
            r => new AgeStatsRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetInt(r, 2)), ct);
        var interPendingAgeStats = await _executor.QueryListAsync(conn, SqlQueries.InterPendingAgeStats, 
            r => new AgeStatsRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetInt(r, 2)), ct);

        return (
            intraPendingCount24h, interPendingCount24h,
            intraPendingCount7d, interPendingCount7d,
            intraPendingOldestSec, interPendingOldestSec,
            intraOpPending, interOpPending,
            intraProgrammed, interProgrammed,
            intraReview, interReview,
            intraPendingBucket, interPendingBucket,
            intraPendingAgeStats, interPendingAgeStats
        );
    }
}
