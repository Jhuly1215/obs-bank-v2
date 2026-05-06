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

        IReadOnlyList<AmountTotalRow> AmountTotal,
        double AmountTotal1h
    )> GetIntraMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        var amountTotal = await _executor.QueryListAsync(conn, SqlQueries.IntraAmountTotal24h, r => new AmountTotalRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetDouble(r, 1)), ct);
        var amountTotal1h = await _executor.ScalarDoubleAsync(conn, SqlQueries.IntraAmountTotal1h, ct);
        return (amountTotal, amountTotal1h);
    }

    public async Task<(
        IReadOnlyList<AmountTotalRow> AmountTotal,
        double AmountTotal1h
    )> GetInterMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        var amountTotal = await _executor.QueryListAsync(conn, SqlQueries.InterAmountTotal24h, r => new AmountTotalRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetDouble(r, 1)), ct);
        var amountTotal1h = await _executor.ScalarDoubleAsync(conn, SqlQueries.InterAmountTotal1h, ct);
        return (amountTotal, amountTotal1h);
    }
}
