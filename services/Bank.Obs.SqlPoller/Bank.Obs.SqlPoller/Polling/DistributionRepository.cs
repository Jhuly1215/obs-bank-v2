using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Bank.Obs.SqlPoller.Polling;

public sealed class DistributionRepository
{
    private readonly ISqlExecutor _executor;

    public DistributionRepository(ISqlExecutor executor)
    {
        _executor = executor;
    }

    public async Task<(
        IReadOnlyList<StateCountRow> StateCount24h,
        IReadOnlyList<TypeCountRow> TypeCount,
        IReadOnlyList<AmountByTypeRow> AmountByType,
        int OtherStateCount24h
    )> GetIntraMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        var stateCount24h = await _executor.QueryListAsync(conn, SqlQueries.IntraStateCount24h, r => new StateCountRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1)), ct);
        var typeCount = await _executor.QueryListAsync(conn, SqlQueries.IntraTypeCount24h, r => new TypeCountRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1)), ct);
        var amountByType = await _executor.QueryListAsync(conn, SqlQueries.IntraAmountByType24h, r => new AmountByTypeRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetDouble(r, 2)), ct);
        var otherStateCount24h = await _executor.ScalarIntAsync(conn, SqlQueries.IntraOtherStateCount24h, ct);
        
        return (stateCount24h, typeCount, amountByType, otherStateCount24h);
    }

    public async Task<(
        IReadOnlyList<StateCountRow> StateCount24h,
        IReadOnlyList<TypeCountRow> TypeCount,
        IReadOnlyList<AmountByTypeRow> AmountByType,
        int OtherStateCount24h
    )> GetInterMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        var stateCount24h = await _executor.QueryListAsync(conn, SqlQueries.InterStateCount24h, r => new StateCountRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1)), ct);
        var typeCount = await _executor.QueryListAsync(conn, SqlQueries.InterTypeCount24h, r => new TypeCountRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1)), ct);
        var amountByType = await _executor.QueryListAsync(conn, SqlQueries.InterAmountByType24h, r => new AmountByTypeRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetDouble(r, 2)), ct);
        var otherStateCount24h = await _executor.ScalarIntAsync(conn, SqlQueries.InterOtherStateCount24h, ct);
        
        return (stateCount24h, typeCount, amountByType, otherStateCount24h);
    }
}
