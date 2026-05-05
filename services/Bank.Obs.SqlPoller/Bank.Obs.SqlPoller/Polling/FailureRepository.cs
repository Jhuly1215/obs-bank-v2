using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Bank.Obs.SqlPoller.Polling;

public sealed class FailureRepository
{
    private readonly ISqlExecutor _executor;

    public FailureRepository(ISqlExecutor executor)
    {
        _executor = executor;
    }

    public async Task<(
        int IntraErrorCount24h, int InterErrorCount24h,
        FailuresRow IntraFailures, FailuresRow InterFailures,
        int IntraZeroDurationCount, int InterZeroDurationCount,
        int IntraMissingModCount, int InterMissingModCount,
        IReadOnlyList<AnomalyRow> QualityAnomalies,
        int IntraCompensatedCurrentCount, int InterCompensatedCurrentCount
    )> GetMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        var intraErrorCount24h = await _executor.ScalarIntAsync(conn, SqlQueries.IntraErrorCount24h, ct);
        var interErrorCount24h = await _executor.ScalarIntAsync(conn, SqlQueries.InterErrorCount24h, ct);

        var intraFailures = await _executor.QuerySingleAsync(conn, SqlQueries.IntraFailures24h, 
            r => new FailuresRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetInt(r, 2)), ct)
            ?? new FailuresRow(0, 0, 0);
        var interFailures = await _executor.QuerySingleAsync(conn, SqlQueries.InterFailures24h, 
            r => new FailuresRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetInt(r, 2)), ct)
            ?? new FailuresRow(0, 0, 0);

        var intraZeroDurationCount = await _executor.ScalarIntAsync(conn, SqlQueries.IntraZeroDurationCount24h, ct);
        var interZeroDurationCount = await _executor.ScalarIntAsync(conn, SqlQueries.InterZeroDurationCount24h, ct);
        
        var intraMissingModCount = await _executor.ScalarIntAsync(conn, SqlQueries.IntraMissingModificationCount24h, ct);
        var interMissingModCount = await _executor.ScalarIntAsync(conn, SqlQueries.InterMissingModificationCount24h, ct);

        var qualityAnomalies = await _executor.QueryListAsync(conn, SqlQueries.QualityAnomalies, 
            r => new AnomalyRow(SqlReaderHelper.GetString(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetInt(r, 2), SqlReaderHelper.GetInt(r, 3), SqlReaderHelper.GetInt(r, 4), SqlReaderHelper.GetInt(r, 5)), ct);

        var intraCompensatedCurrentCount = await _executor.ScalarIntAsync(conn, SqlQueries.IntraCompensatedCurrentCount, ct);
        var interCompensatedCurrentCount = await _executor.ScalarIntAsync(conn, SqlQueries.InterCompensatedCurrentCount, ct);

        return (
            intraErrorCount24h, interErrorCount24h,
            intraFailures, interFailures,
            intraZeroDurationCount, interZeroDurationCount,
            intraMissingModCount, interMissingModCount,
            qualityAnomalies,
            intraCompensatedCurrentCount, interCompensatedCurrentCount
        );
    }
}
