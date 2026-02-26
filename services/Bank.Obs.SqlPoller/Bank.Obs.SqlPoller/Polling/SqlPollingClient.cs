using Microsoft.Data.SqlClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bank.Obs.SqlPoller.Polling;

public sealed class SqlPollingClient
{
    public async Task<Snapshot> PollAsync(string connString, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connString);
        await conn.OpenAsync(ct);

        var intraTxLast30d = await ScalarIntAsync(conn, SqlQueries.IntraTxLast30d, ct);
        var interTxLast30d = await ScalarIntAsync(conn, SqlQueries.InterTxLast30d, ct);

        var intraPendingLast7d = await ScalarIntAsync(conn, SqlQueries.IntraPendingLast7d, ct);
        var interPendingLast7d = await ScalarIntAsync(conn, SqlQueries.InterPendingProxyLast7d, ct);

        var intraFailTechRate30d = await ScalarDoubleAsync(conn, SqlQueries.IntraFailTechRate30d, ct);
        var interFailTechRate30d = await ScalarDoubleAsync(conn, SqlQueries.InterFailTechRate30d, ct);

        var interState9ObservedShare30d = await ScalarDoubleAsync(conn, SqlQueries.InterState9ObservedShare30d, ct);

        var intraPendingMaxAgeMin = await ScalarDoubleAsync(conn, SqlQueries.IntraPendingMaxAgeMin, ct);
        var interPendingMaxAgeMin = await ScalarDoubleAsync(conn, SqlQueries.InterPendingMaxAgeMin, ct);

        return new Snapshot(
            intraTxLast30d,
            interTxLast30d,
            intraPendingLast7d,
            interPendingLast7d,
            intraFailTechRate30d,
            interFailTechRate30d,
            interState9ObservedShare30d,
            intraPendingMaxAgeMin,
            interPendingMaxAgeMin);
    }

    private static async Task<int> ScalarIntAsync(SqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 30 };
        var obj = await cmd.ExecuteScalarAsync(ct);
        return obj == null || obj == DBNull.Value ? 0 : Convert.ToInt32(obj);
    }

    private static async Task<double> ScalarDoubleAsync(SqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 30 };
        var obj = await cmd.ExecuteScalarAsync(ct);
        return obj == null || obj == DBNull.Value ? 0 : Convert.ToDouble(obj);
    }

    public sealed record Snapshot(
        int IntraTxLast30d,
        int InterTxLast30d,
        int IntraPendingLast7d,
        int InterPendingLast7d, 
        double IntraFailTechRate30d,
        double InterFailTechRate30d,
        double IntraState9InterShare30d,
        double IntraPendingMaxAgeMin,
        double InterPendingMaxAgeMin);
}