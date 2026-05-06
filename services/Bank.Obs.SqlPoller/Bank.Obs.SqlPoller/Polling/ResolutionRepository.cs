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
        int IntraResolvedCount24h, int InterResolvedCount24h,
        int IntraResAvgSec, int InterResAvgSec,
        IReadOnlyList<SpeedStatsRow> IntraSuccessSpeed, IReadOnlyList<SpeedStatsRow> InterSuccessSpeed,
        int IntraSuccessCount24h, int InterSuccessCount24h,
        int IntraSuccessSpeedP99, int InterSuccessSpeedP99,
        int IntraClosedCount24h, int InterClosedCount24h
    )> GetMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        var intraResolvedCount24h = await _executor.ScalarIntAsync(conn, SqlQueries.IntraResolvedCount24h, ct);
        var interResolvedCount24h = await _executor.ScalarIntAsync(conn, SqlQueries.InterResolvedCount24h, ct);
        var intraResAvgSec = await _executor.ScalarIntAsync(conn, SqlQueries.IntraResolutionAvgSeconds, ct);
        var interResAvgSec = await _executor.ScalarIntAsync(conn, SqlQueries.InterResolutionAvgSeconds, ct);

        var intraSuccessSpeed = await _executor.QueryListAsync(conn, SqlQueries.IntraSuccessSpeed24h, 
            r => new SpeedStatsRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), System.Convert.ToInt32(r.GetValue(2))), ct);
        var interSuccessSpeed = await _executor.QueryListAsync(conn, SqlQueries.InterSuccessSpeed24h, 
            r => new SpeedStatsRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), System.Convert.ToInt32(r.GetValue(2))), ct);

        var intraSuccessCount24h = await _executor.ScalarIntAsync(conn, SqlQueries.IntraSuccessCount24h, ct);
        var interSuccessCount24h = await _executor.ScalarIntAsync(conn, SqlQueries.InterSuccessCount24h, ct);
        
        var intraSuccessSpeedP99 = await _executor.ScalarIntAsync(conn, SqlQueries.IntraSuccessSpeedP99_24h, ct);
        var interSuccessSpeedP99 = await _executor.ScalarIntAsync(conn, SqlQueries.InterSuccessSpeedP99_24h, ct);

        var intraClosedCount24h = await _executor.ScalarIntAsync(conn, SqlQueries.IntraClosedCount24h, ct);
        var interClosedCount24h = await _executor.ScalarIntAsync(conn, SqlQueries.InterClosedCount24h, ct);

        return (
            intraResolvedCount24h, interResolvedCount24h,
            intraResAvgSec, interResAvgSec,
            intraSuccessSpeed, interSuccessSpeed,
            intraSuccessCount24h, interSuccessCount24h,
            intraSuccessSpeedP99, interSuccessSpeedP99,
            intraClosedCount24h, interClosedCount24h
        );
    }
}
