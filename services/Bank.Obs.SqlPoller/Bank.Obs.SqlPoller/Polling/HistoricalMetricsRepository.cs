using Microsoft.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;


namespace Bank.Obs.SqlPoller.Polling;

public sealed class HistoricalMetricsRepository
{
    private readonly ISqlExecutor _executor;

    public HistoricalMetricsRepository(ISqlExecutor executor)
    {
        _executor = executor;
    }

    public async Task<(
        int TxCreated5m, int TxCreated15m, int TxCreated1h, int TxCreated24h, int TxCreated7d, int TxCreated30d
    )> GetIntraMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        var m5 = await _executor.ScalarIntAsync(conn, SqlQueries.IntraTxCreated5m, ct);
        var m15 = await _executor.ScalarIntAsync(conn, SqlQueries.IntraTxCreated15m, ct);
        var h1 = await _executor.ScalarIntAsync(conn, SqlQueries.IntraTxCreated1h, ct);
        var h24 = await _executor.ScalarIntAsync(conn, SqlQueries.IntraTxCreated24h, ct);
        var d7 = await _executor.ScalarIntAsync(conn, SqlQueries.IntraTxCreated7d, ct);
        var d30 = await _executor.ScalarIntAsync(conn, SqlQueries.IntraTxCreated30d, ct);
        return (m5, m15, h1, h24, d7, d30);
    }

    public async Task<(
        int TxCreated5m, int TxCreated15m, int TxCreated1h, int TxCreated24h, int TxCreated7d, int TxCreated30d
    )> GetInterMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        var m5 = await _executor.ScalarIntAsync(conn, SqlQueries.InterTxCreated5m, ct);
        var m15 = await _executor.ScalarIntAsync(conn, SqlQueries.InterTxCreated15m, ct);
        var h1 = await _executor.ScalarIntAsync(conn, SqlQueries.InterTxCreated1h, ct);
        var h24 = await _executor.ScalarIntAsync(conn, SqlQueries.InterTxCreated24h, ct);
        var d7 = await _executor.ScalarIntAsync(conn, SqlQueries.InterTxCreated7d, ct);
        var d30 = await _executor.ScalarIntAsync(conn, SqlQueries.InterTxCreated30d, ct);
        return (m5, m15, h1, h24, d7, d30);
    }
}
