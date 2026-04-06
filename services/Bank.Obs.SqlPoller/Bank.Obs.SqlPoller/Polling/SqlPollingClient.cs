using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
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
        // Historical Scalars (Keep for base continuity)
        // =========================
        var intraTxCreated15m = await ScalarIntAsync(conn, SqlQueries.IntraTxCreated15m, ct);
        var interTxCreated15m = await ScalarIntAsync(conn, SqlQueries.InterTxCreated15m, ct);
        var intraTxCreated24h = await ScalarIntAsync(conn, SqlQueries.IntraTxCreated24h, ct);
        var interTxCreated24h = await ScalarIntAsync(conn, SqlQueries.InterTxCreated24h, ct);
        var intraTxCreated7d = await ScalarIntAsync(conn, SqlQueries.IntraTxCreated7d, ct);
        var interTxCreated7d = await ScalarIntAsync(conn, SqlQueries.InterTxCreated7d, ct);
        var intraTxCreated30d = await ScalarIntAsync(conn, SqlQueries.IntraTxCreated30d, ct);
        var interTxCreated30d = await ScalarIntAsync(conn, SqlQueries.InterTxCreated30d, ct);

        var intraPendingCount24h = await ScalarIntAsync(conn, SqlQueries.IntraPendingCount24h, ct);
        var interPendingCount24h = await ScalarIntAsync(conn, SqlQueries.InterPendingCount24h, ct);
        var intraPendingCount7d = await ScalarIntAsync(conn, SqlQueries.IntraPendingCount7d, ct);
        var interPendingCount7d = await ScalarIntAsync(conn, SqlQueries.InterPendingCount7d, ct);
        var intraPendingOldestSec = await ScalarIntAsync(conn, SqlQueries.IntraPendingOldestSeconds, ct);
        var interPendingOldestSec = await ScalarIntAsync(conn, SqlQueries.InterPendingOldestSeconds, ct);

        var intraErrorCount24h = await ScalarIntAsync(conn, SqlQueries.IntraErrorCount24h, ct);
        var interErrorCount24h = await ScalarIntAsync(conn, SqlQueries.InterErrorCount24h, ct);
        var intraResolvedCount24h = await ScalarIntAsync(conn, SqlQueries.IntraResolvedCount24h, ct);
        var interResolvedCount24h = await ScalarIntAsync(conn, SqlQueries.InterResolvedCount24h, ct);
        var intraResAvgSec = await ScalarIntAsync(conn, SqlQueries.IntraResolutionAvgSeconds, ct);
        var interResAvgSec = await ScalarIntAsync(conn, SqlQueries.InterResolutionAvgSeconds, ct);

        // =========================
        // Tabular Metrics (Grouped)
        // =========================

        // 1) State Count 24h
        var intraStateCount24h = await QueryListAsync(conn, SqlQueries.IntraStateCount24h, 
            r => new StateCountRow(r.GetInt32(0), r.GetInt32(1)), ct);
        var interStateCount24h = await QueryListAsync(conn, SqlQueries.InterStateCount24h, 
            r => new StateCountRow(r.GetInt32(0), r.GetInt32(1)), ct);

        // 2) Pending Current
        var intraPendingCurrent = await QueryListAsync(conn, SqlQueries.IntraPendingCurrentCount, 
            r => new StateCountRow(r.GetInt32(0), r.GetInt32(1)), ct);
        var interPendingCurrent = await QueryListAsync(conn, SqlQueries.InterPendingCurrentCount, 
            r => new StateCountRow(r.GetInt32(0), r.GetInt32(1)), ct);

        // 3) Pending Aging Bucket
        var intraPendingBucket = await QueryListAsync(conn, SqlQueries.IntraPendingAgingBucketCount, 
            r => new PendingBucketRow(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetInt32(3)), ct);
        var interPendingBucket = await QueryListAsync(conn, SqlQueries.InterPendingAgingBucketCount, 
            r => new PendingBucketRow(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetInt32(3)), ct);

        // 4) Pending Age Stats
        var intraAgeStats = await QueryListAsync(conn, SqlQueries.IntraPendingAgeStats, 
            r => new AgeStatsRow(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2)), ct);
        var interAgeStats = await QueryListAsync(conn, SqlQueries.InterPendingAgeStats, 
            r => new AgeStatsRow(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2)), ct);

        // 5) Failures Specific 24h
        var intraFailures = await QuerySingleAsync(conn, SqlQueries.IntraFailures24h, 
            r => new FailuresRow(r.GetInt32(0), r.GetInt32(1)), ct);
        var interFailures = await QuerySingleAsync(conn, SqlQueries.InterFailures24h, 
            r => new FailuresRow(r.GetInt32(0), r.GetInt32(1)), ct);

        // 6) Type Count 24h
        var intraTypeCount = await QueryListAsync(conn, SqlQueries.IntraTypeCount24h, 
            r => new TypeCountRow(r.GetInt32(0), r.GetInt32(1)), ct);
        var interTypeCount = await QueryListAsync(conn, SqlQueries.InterTypeCount24h, 
            r => new TypeCountRow(r.GetInt32(0), r.GetInt32(1)), ct);

        // 7) Amount Total 24h
        var intraAmountTotal = await QueryListAsync(conn, SqlQueries.IntraAmountTotal24h, 
            r => new AmountTotalRow(r.GetInt32(0), Convert.ToDouble(r.GetValue(1))), ct);
        var interAmountTotal = await QueryListAsync(conn, SqlQueries.InterAmountTotal24h, 
            r => new AmountTotalRow(r.GetInt32(0), Convert.ToDouble(r.GetValue(1))), ct);

        // 8) Amount Type x Moneda 24h
        var intraAmountByType = await QueryListAsync(conn, SqlQueries.IntraAmountByType24h, 
            r => new AmountByTypeRow(r.GetInt32(0), r.GetInt32(1), Convert.ToDouble(r.GetValue(2))), ct);
        var interAmountByType = await QueryListAsync(conn, SqlQueries.InterAmountByType24h, 
            r => new AmountByTypeRow(r.GetInt32(0), r.GetInt32(1), Convert.ToDouble(r.GetValue(2))), ct);

        // 9) Success Speed proxy 24h
        var intraSuccessSpeed = await QueryListAsync(conn, SqlQueries.IntraSuccessSpeed24h, 
            r => new SpeedStatsRow(r.GetInt32(0), r.GetInt32(1), Convert.ToInt32(r.GetValue(2))), ct);
        var interSuccessSpeed = await QueryListAsync(conn, SqlQueries.InterSuccessSpeed24h, 
            r => new SpeedStatsRow(r.GetInt32(0), r.GetInt32(1), Convert.ToInt32(r.GetValue(2))), ct);

        // 10) Interbank destination specifics
        var interBankCount = await QueryListAsync(conn, SqlQueries.InterBankCount24h, 
            r => new BankCountRow(r.GetInt32(0), r.GetInt32(1)), ct);
        var interBankStateCount = await QueryListAsync(conn, SqlQueries.InterBankStateCount24h, 
            r => new BankStateCountRow(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2)), ct);
        var interBankAmountTotal = await QueryListAsync(conn, SqlQueries.InterBankAmountTotal24h, 
            r => new BankAmountRow(r.GetInt32(0), r.GetInt32(1), Convert.ToDouble(r.GetValue(2))), ct);

        return new Snapshot(
            intraTxCreated15m, interTxCreated15m,
            intraTxCreated24h, interTxCreated24h,
            intraTxCreated7d, interTxCreated7d,
            intraTxCreated30d, interTxCreated30d,
            intraPendingCount24h, interPendingCount24h,
            intraPendingCount7d, interPendingCount7d,
            intraPendingOldestSec, interPendingOldestSec,
            intraErrorCount24h, interErrorCount24h,
            intraResolvedCount24h, interResolvedCount24h,
            intraResAvgSec, interResAvgSec,

            intraStateCount24h, interStateCount24h,
            intraPendingCurrent, interPendingCurrent,
            intraPendingBucket, interPendingBucket,
            intraAgeStats, interAgeStats,
            intraFailures, interFailures,
            intraTypeCount, interTypeCount,
            intraAmountTotal, interAmountTotal,
            intraAmountByType, interAmountByType,
            intraSuccessSpeed, interSuccessSpeed,
            
            interBankCount, interBankStateCount, interBankAmountTotal
        );
    }

    private static async Task<int> ScalarIntAsync(SqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 30 };
        var obj = await cmd.ExecuteScalarAsync(ct);
        return obj == null || obj == DBNull.Value ? 0 : Convert.ToInt32(obj);
    }

    private static async Task<List<T>> QueryListAsync<T>(SqlConnection conn, string sql, Func<SqlDataReader, T> map, CancellationToken ct)
    {
        var list = new List<T>();
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            list.Add(map(r));
        }
        return list;
    }

    private static async Task<T> QuerySingleAsync<T>(SqlConnection conn, string sql, Func<SqlDataReader, T> map, CancellationToken ct) where T: class
    {
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (await r.ReadAsync(ct)) return map(r);
        return null;
    }

    // DTOs Tabulares
    public sealed record StateCountRow(int Estado, int Count);
    public sealed record PendingBucketRow(int Estado, int Ge14400s, int Ge3600s, int Ge900s);
    public sealed record AgeStatsRow(int Estado, int AvgSec, int MaxSec);
    public sealed record FailuresRow(int Rejected, int FailedTechnical);
    public sealed record TypeCountRow(int Tipo, int Count);
    public sealed record AmountTotalRow(int Moneda, double Total);
    public sealed record AmountByTypeRow(int Tipo, int Moneda, double Total);
    public sealed record SpeedStatsRow(int Tipo, int AvgSec, int P95Sec);
    public sealed record BankCountRow(int Banco, int Count);
    public sealed record BankStateCountRow(int Banco, int Estado, int Count);
    public sealed record BankAmountRow(int Banco, int Moneda, double Total);

    public sealed record Snapshot(
        // Historical Scalars
        int IntraTxCreated15m, int InterTxCreated15m,
        int IntraTxCreated24h, int InterTxCreated24h,
        int IntraTxCreated7d, int InterTxCreated7d,
        int IntraTxCreated30d, int InterTxCreated30d,
        int IntraPendingCount24h, int InterPendingCount24h,
        int IntraPendingCount7d, int InterPendingCount7d,
        int IntraPendingOldestSec, int InterPendingOldestSec,
        int IntraErrorCount24h, int InterErrorCount24h,
        int IntraResolvedCount24h, int InterResolvedCount24h,
        int IntraResAvgSec, int InterResAvgSec,

        // Tabular Collections
        IReadOnlyList<StateCountRow> IntraStateCount24h, IReadOnlyList<StateCountRow> InterStateCount24h,
        IReadOnlyList<StateCountRow> IntraPendingCurrent, IReadOnlyList<StateCountRow> InterPendingCurrent,
        IReadOnlyList<PendingBucketRow> IntraPendingBucket, IReadOnlyList<PendingBucketRow> InterPendingBucket,
        IReadOnlyList<AgeStatsRow> IntraAgeStats, IReadOnlyList<AgeStatsRow> InterAgeStats,
        FailuresRow IntraFailures, FailuresRow InterFailures,
        IReadOnlyList<TypeCountRow> IntraTypeCount, IReadOnlyList<TypeCountRow> InterTypeCount,
        IReadOnlyList<AmountTotalRow> IntraAmountTotal, IReadOnlyList<AmountTotalRow> InterAmountTotal,
        IReadOnlyList<AmountByTypeRow> IntraAmountByType, IReadOnlyList<AmountByTypeRow> InterAmountByType,
        IReadOnlyList<SpeedStatsRow> IntraSuccessSpeed, IReadOnlyList<SpeedStatsRow> InterSuccessSpeed,

        // Interbank Details
        IReadOnlyList<BankCountRow> InterBankCount,
        IReadOnlyList<BankStateCountRow> InterBankStateCount,
        IReadOnlyList<BankAmountRow> InterBankAmountTotal
    );
}