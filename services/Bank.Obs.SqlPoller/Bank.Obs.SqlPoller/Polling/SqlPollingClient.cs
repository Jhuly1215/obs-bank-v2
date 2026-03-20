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

        // =========================
        // Volumen
        // =========================
        var intraTxLast15m = await ScalarIntAsync(conn, SqlQueries.IntraTxLast15m, ct);
        var interTxLast15m = await ScalarIntAsync(conn, SqlQueries.InterTxLast15m, ct);

        var intraTxLast24h = await ScalarIntAsync(conn, SqlQueries.IntraTxLast24h, ct);
        var interTxLast24h = await ScalarIntAsync(conn, SqlQueries.InterTxLast24h, ct);

        var intraTxLast30d = await ScalarIntAsync(conn, SqlQueries.IntraTxLast30d, ct);
        var interTxLast30d = await ScalarIntAsync(conn, SqlQueries.InterTxLast30d, ct);

        // =========================
        // Backlog / pendientes
        // (Regla: estado 9 = error, NO pendiente)
        // =========================
        var intraPendingCount24h = await ScalarIntAsync(conn, SqlQueries.IntraPendingCount24h, ct);
        var interPendingCount24h = await ScalarIntAsync(conn, SqlQueries.InterPendingCount24h, ct);

        var intraPendingLast7d = await ScalarIntAsync(conn, SqlQueries.IntraPendingLast7d, ct);
        var interPendingLast7d = await ScalarIntAsync(conn, SqlQueries.InterPendingLast7d, ct);

        var intraPendingMaxAgeMin = await ScalarDoubleAsync(conn, SqlQueries.IntraPendingMaxAgeMin, ct);
        var interPendingMaxAgeMin = await ScalarDoubleAsync(conn, SqlQueries.InterPendingMaxAgeMin, ct);

        // =========================
        // Calidad / errores (rates)
        // =========================
        var intraFailTechRate24h = await ScalarDoubleAsync(conn, SqlQueries.IntraFailTechRate24h, ct);
        var interFailTechRate24h = await ScalarDoubleAsync(conn, SqlQueries.InterFailTechRate24h, ct);

        var intraFailTechRate30d = await ScalarDoubleAsync(conn, SqlQueries.IntraFailTechRate30d, ct);
        var interFailTechRate30d = await ScalarDoubleAsync(conn, SqlQueries.InterFailTechRate30d, ct);

        var interErrorState9Share24h = await ScalarDoubleAsync(conn, SqlQueries.InterErrorState9Share24h, ct);
        var interErrorState9Share30d = await ScalarDoubleAsync(conn, SqlQueries.InterErrorState9Share30d, ct);

        // =========================
        // Errores (conteos + edad)
        // =========================
        var intraErrorCount24h = await ScalarIntAsync(conn, SqlQueries.IntraErrorCount24h, ct);
        var interErrorCount24h = await ScalarIntAsync(conn, SqlQueries.InterErrorCount24h, ct);

        var intraErrorMaxAgeMin7d = await ScalarDoubleAsync(conn, SqlQueries.IntraErrorMaxAgeMin7d, ct);
        var interErrorMaxAgeMin7d = await ScalarDoubleAsync(conn, SqlQueries.InterErrorMaxAgeMin7d, ct);

        return new Snapshot(
            intraTxLast15m,
            interTxLast15m,
            intraTxLast24h,
            interTxLast24h,
            intraTxLast30d,
            interTxLast30d,
            intraPendingCount24h,
            interPendingCount24h,
            intraPendingLast7d,
            interPendingLast7d,
            intraPendingMaxAgeMin,
            interPendingMaxAgeMin,
            intraFailTechRate24h,
            interFailTechRate24h,
            intraFailTechRate30d,
            interFailTechRate30d,
            interErrorState9Share24h,
            interErrorState9Share30d,
            intraErrorCount24h,
            interErrorCount24h,
            intraErrorMaxAgeMin7d,
            interErrorMaxAgeMin7d);
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
        // Volumen
        int IntraTxLast15m,
        int InterTxLast15m,
        int IntraTxLast24h,
        int InterTxLast24h,
        int IntraTxLast30d,
        int InterTxLast30d,

        // Pendientes
        int IntraPendingCount24h,
        int InterPendingCount24h,
        int IntraPendingLast7d,
        int InterPendingLast7d,
        double IntraPendingMaxAgeMin,
        double InterPendingMaxAgeMin,

        // Rates
        double IntraFailTechRate24h,
        double InterFailTechRate24h,
        double IntraFailTechRate30d,
        double InterFailTechRate30d,
        double InterErrorState9Share24h,
        double InterErrorState9Share30d,

        // Errores
        int IntraErrorCount24h,
        int InterErrorCount24h,
        double IntraErrorMaxAgeMin7d,
        double InterErrorMaxAgeMin7d);
}