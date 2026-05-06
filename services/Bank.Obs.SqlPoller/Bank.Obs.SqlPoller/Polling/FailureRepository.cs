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
        int ErrorCount24h, FailuresRow Failures,
        int ZeroDurationCount, int MissingModCount,
        int CompensatedCurrentCount
    )> GetIntraMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        var errorCount24h = await _executor.ScalarIntAsync(conn, SqlQueries.IntraErrorCount24h, ct);
        var failures = await _executor.QuerySingleAsync(conn, SqlQueries.IntraFailures24h, r => new FailuresRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetInt(r, 2)), ct) ?? new FailuresRow(0, 0, 0);
        var zeroDurationCount = await _executor.ScalarIntAsync(conn, SqlQueries.IntraZeroDurationCount24h, ct);
        var missingModCount = await _executor.ScalarIntAsync(conn, SqlQueries.IntraMissingModificationCount24h, ct);
        var compensatedCurrentCount = await _executor.ScalarIntAsync(conn, SqlQueries.IntraCompensatedCurrentCount, ct);
        
        return (errorCount24h, failures, zeroDurationCount, missingModCount, compensatedCurrentCount);
    }

    public async Task<(
        int ErrorCount24h, FailuresRow Failures,
        int ZeroDurationCount, int MissingModCount,
        int CompensatedCurrentCount
    )> GetInterMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        var errorCount24h = await _executor.ScalarIntAsync(conn, SqlQueries.InterErrorCount24h, ct);
        var failures = await _executor.QuerySingleAsync(conn, SqlQueries.InterFailures24h, r => new FailuresRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetInt(r, 2)), ct) ?? new FailuresRow(0, 0, 0);
        var zeroDurationCount = await _executor.ScalarIntAsync(conn, SqlQueries.InterZeroDurationCount24h, ct);
        var missingModCount = await _executor.ScalarIntAsync(conn, SqlQueries.InterMissingModificationCount24h, ct);
        var compensatedCurrentCount = await _executor.ScalarIntAsync(conn, SqlQueries.InterCompensatedCurrentCount, ct);
        
        return (errorCount24h, failures, zeroDurationCount, missingModCount, compensatedCurrentCount);
    }

    public async Task<IReadOnlyList<AnomalyRow>> GetSystemMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        return await _executor.QueryListAsync(conn, SqlQueries.QualityAnomalies, 
            r => new AnomalyRow(SqlReaderHelper.GetString(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetInt(r, 2), SqlReaderHelper.GetInt(r, 3), SqlReaderHelper.GetInt(r, 4), SqlReaderHelper.GetInt(r, 5)), ct);
    }
}
