using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Bank.Obs.SqlPoller.Polling;

public sealed class AmountRepository
{
    private readonly ISqlExecutor _executor;

    public AmountRepository(ISqlExecutor executor)
    {
        _executor = executor;
    }

    public async Task<(
        IReadOnlyList<AmountTotalRow> IntraAmountTotal, IReadOnlyList<AmountTotalRow> InterAmountTotal,
        double IntraAmountTotal1h, double InterAmountTotal1h
    )> GetMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        var intraAmountTotal = await _executor.QueryListAsync(conn, SqlQueries.IntraAmountTotal24h, 
            r => new AmountTotalRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetDouble(r, 1)), ct);
        var interAmountTotal = await _executor.QueryListAsync(conn, SqlQueries.InterAmountTotal24h, 
            r => new AmountTotalRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetDouble(r, 1)), ct);

        var intraAmountTotal1h = await _executor.ScalarDoubleAsync(conn, SqlQueries.IntraAmountTotal1h, ct);
        var interAmountTotal1h = await _executor.ScalarDoubleAsync(conn, SqlQueries.InterAmountTotal1h, ct);

        return (
            intraAmountTotal, interAmountTotal,
            intraAmountTotal1h, interAmountTotal1h
        );
    }
}
