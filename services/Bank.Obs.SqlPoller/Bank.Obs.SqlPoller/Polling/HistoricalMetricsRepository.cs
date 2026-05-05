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
        int IntraTxCreated5m, int InterTxCreated5m,
        int IntraTxCreated15m, int InterTxCreated15m,
        int IntraTxCreated1h, int InterTxCreated1h,
        int IntraTxCreated24h, int InterTxCreated24h,
        int IntraTxCreated7d, int InterTxCreated7d,
        int IntraTxCreated30d, int InterTxCreated30d
    )> GetMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        var intraTxCreated5m = await _executor.ScalarIntAsync(conn, SqlQueries.IntraTxCreated5m, ct);
        var interTxCreated5m = await _executor.ScalarIntAsync(conn, SqlQueries.InterTxCreated5m, ct);
        var intraTxCreated15m = await _executor.ScalarIntAsync(conn, SqlQueries.IntraTxCreated15m, ct);
        var interTxCreated15m = await _executor.ScalarIntAsync(conn, SqlQueries.InterTxCreated15m, ct);
        var intraTxCreated1h = await _executor.ScalarIntAsync(conn, SqlQueries.IntraTxCreated1h, ct);
        var interTxCreated1h = await _executor.ScalarIntAsync(conn, SqlQueries.InterTxCreated1h, ct);
        var intraTxCreated24h = await _executor.ScalarIntAsync(conn, SqlQueries.IntraTxCreated24h, ct);
        var interTxCreated24h = await _executor.ScalarIntAsync(conn, SqlQueries.InterTxCreated24h, ct);
        var intraTxCreated7d = await _executor.ScalarIntAsync(conn, SqlQueries.IntraTxCreated7d, ct);
        var interTxCreated7d = await _executor.ScalarIntAsync(conn, SqlQueries.InterTxCreated7d, ct);
        var intraTxCreated30d = await _executor.ScalarIntAsync(conn, SqlQueries.IntraTxCreated30d, ct);
        var interTxCreated30d = await _executor.ScalarIntAsync(conn, SqlQueries.InterTxCreated30d, ct);

        return (
            intraTxCreated5m, interTxCreated5m,
            intraTxCreated15m, interTxCreated15m,
            intraTxCreated1h, interTxCreated1h,
            intraTxCreated24h, interTxCreated24h,
            intraTxCreated7d, interTxCreated7d,
            intraTxCreated30d, interTxCreated30d
        );
    }
}
