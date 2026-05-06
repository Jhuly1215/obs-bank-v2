using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Bank.Obs.SqlPoller.Polling;

public sealed class ResolutionRepository
{
    private readonly ISqlExecutor _executor;

    public ResolutionRepository(ISqlExecutor executor)
    {
        _executor = executor;
    }

    public async Task<(
        int ResolvedCount24h, int ResAvgSec,
        IReadOnlyList<SpeedStatsRow> SuccessSpeed,
        int SuccessCount24h, int SuccessSpeedP99, int ClosedCount24h
    )> GetIntraMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        var resolvedCount = await _executor.ScalarIntAsync(conn, SqlQueries.IntraResolvedCount24h, ct);
        var resAvgSec = await _executor.ScalarIntAsync(conn, SqlQueries.IntraResolutionAvgSeconds, ct);
        var successSpeed = await _executor.QueryListAsync(conn, SqlQueries.IntraSuccessSpeed24h, r => new SpeedStatsRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), System.Convert.ToInt32(r.GetValue(2))), ct);
        var successCount = await _executor.ScalarIntAsync(conn, SqlQueries.IntraSuccessCount24h, ct);
        var successSpeedP99 = await _executor.ScalarIntAsync(conn, SqlQueries.IntraSuccessSpeedP99_24h, ct);
        var closedCount = await _executor.ScalarIntAsync(conn, SqlQueries.IntraClosedCount24h, ct);
        
        return (resolvedCount, resAvgSec, successSpeed, successCount, successSpeedP99, closedCount);
    }

    public async Task<(
        int ResolvedCount24h, int ResAvgSec,
        IReadOnlyList<SpeedStatsRow> SuccessSpeed,
        int SuccessCount24h, int SuccessSpeedP99, int ClosedCount24h
    )> GetInterMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        var resolvedCount = await _executor.ScalarIntAsync(conn, SqlQueries.InterResolvedCount24h, ct);
        var resAvgSec = await _executor.ScalarIntAsync(conn, SqlQueries.InterResolutionAvgSeconds, ct);
        var successSpeed = await _executor.QueryListAsync(conn, SqlQueries.InterSuccessSpeed24h, r => new SpeedStatsRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), System.Convert.ToInt32(r.GetValue(2))), ct);
        var successCount = await _executor.ScalarIntAsync(conn, SqlQueries.InterSuccessCount24h, ct);
        var successSpeedP99 = await _executor.ScalarIntAsync(conn, SqlQueries.InterSuccessSpeedP99_24h, ct);
        var closedCount = await _executor.ScalarIntAsync(conn, SqlQueries.InterClosedCount24h, ct);
        
        return (resolvedCount, resAvgSec, successSpeed, successCount, successSpeedP99, closedCount);
    }
}
