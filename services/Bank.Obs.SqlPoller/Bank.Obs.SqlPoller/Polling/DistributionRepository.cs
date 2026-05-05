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
        IReadOnlyList<StateCountRow> IntraStateCount24h, IReadOnlyList<StateCountRow> InterStateCount24h,
        IReadOnlyList<TypeCountRow> IntraTypeCount, IReadOnlyList<TypeCountRow> InterTypeCount,
        IReadOnlyList<AmountByTypeRow> IntraAmountByType, IReadOnlyList<AmountByTypeRow> InterAmountByType,
        int IntraOtherStateCount24h, int InterOtherStateCount24h
    )> GetMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        var intraStateCount24h = await _executor.QueryListAsync(conn, SqlQueries.IntraStateCount24h, 
            r => new StateCountRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1)), ct);
        var interStateCount24h = await _executor.QueryListAsync(conn, SqlQueries.InterStateCount24h, 
            r => new StateCountRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1)), ct);

        var intraTypeCount = await _executor.QueryListAsync(conn, SqlQueries.IntraTypeCount24h, 
            r => new TypeCountRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1)), ct);
        var interTypeCount = await _executor.QueryListAsync(conn, SqlQueries.InterTypeCount24h, 
            r => new TypeCountRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1)), ct);

        var intraAmountByType = await _executor.QueryListAsync(conn, SqlQueries.IntraAmountByType24h, 
            r => new AmountByTypeRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetDouble(r, 2)), ct);
        var interAmountByType = await _executor.QueryListAsync(conn, SqlQueries.InterAmountByType24h, 
            r => new AmountByTypeRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetDouble(r, 2)), ct);

        var intraOtherStateCount24h = await _executor.ScalarIntAsync(conn, SqlQueries.IntraOtherStateCount24h, ct);
        var interOtherStateCount24h = await _executor.ScalarIntAsync(conn, SqlQueries.InterOtherStateCount24h, ct);

        return (
            intraStateCount24h, interStateCount24h,
            intraTypeCount, interTypeCount,
            intraAmountByType, interAmountByType,
            intraOtherStateCount24h, interOtherStateCount24h
        );
    }
}
