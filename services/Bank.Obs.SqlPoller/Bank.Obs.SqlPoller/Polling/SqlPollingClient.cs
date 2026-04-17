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

        // 0) Context
        var dayTypeObj = await new SqlCommand(SqlQueries.GetDayType, conn).ExecuteScalarAsync(ct);
        var dayType = dayTypeObj?.ToString() ?? "habil";

        // =========================
        // Historical Scalars (Keep for base continuity)
        // =========================
        var intraTxCreated15m = await ScalarIntAsync(conn, SqlQueries.IntraTxCreated15m, ct);
        var interTxCreated15m = await ScalarIntAsync(conn, SqlQueries.InterTxCreated15m, ct);
        var intraTxCreated24h = await ScalarIntAsync(conn, SqlQueries.IntraTxCreated24h, ct);
        var interTxCreated24h = await ScalarIntAsync(conn, SqlQueries.InterTxCreated24h, ct);
        
        // These are more expensive, keeping them for now but they should be moved to offline analysis
        var intraTxCreated7d = await ScalarIntAsync(conn, SqlQueries.IntraTxCreated7d, ct);
        var interTxCreated7d = await ScalarIntAsync(conn, SqlQueries.InterTxCreated7d, ct);
        var intraTxCreated30d = await ScalarIntAsync(conn, SqlQueries.IntraTxCreated30d, ct);
        var interTxCreated30d = await ScalarIntAsync(conn, SqlQueries.InterTxCreated30d, ct);

        // Standard backlog count (Legacy compatibility)
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
        // Tabular & Operational (Refactored)
        // =========================

        // 1) Distribution by state 24h
        var intraStateCount24h = await QueryListAsync(conn, SqlQueries.IntraStateCount24h, 
            r => new StateCountRow(GetInt(r, 0), GetInt(r, 1)), ct);
        var interStateCount24h = await QueryListAsync(conn, SqlQueries.InterStateCount24h, 
            r => new StateCountRow(GetInt(r, 0), GetInt(r, 1)), ct);

        // 2) Backlogs (Op vs Programmed)
        var intraOpPending = await QueryListAsync(conn, SqlQueries.IntraOpPendingCount, 
            r => new StateCountRow(GetInt(r, 0), GetInt(r, 1)), ct);
        var interOpPending = await QueryListAsync(conn, SqlQueries.InterOpPendingCount, 
            r => new StateCountRow(GetInt(r, 0), GetInt(r, 1)), ct);
        
        var intraProgrammed = await QueryListAsync(conn, SqlQueries.IntraProgrammedCount, 
            r => new StateCountRow(GetInt(r, 0), GetInt(r, 1)), ct);
        var interProgrammed = await QueryListAsync(conn, SqlQueries.InterProgrammedCount, 
            r => new StateCountRow(GetInt(r, 0), GetInt(r, 1)), ct);

        // 3) Review (State 100)
        var intraReview = await QuerySingleAsync(conn, SqlQueries.IntraReviewStats, 
            r => new ReviewStatsRow(GetInt(r, 0), GetInt(r, 1), GetInt(r, 2), GetInt(r, 3)), ct) 
            ?? new ReviewStatsRow(0, 0, 0, 0);
        var interReview = await QuerySingleAsync(conn, SqlQueries.InterReviewStats, 
            r => new ReviewStatsRow(GetInt(r, 0), GetInt(r, 1), GetInt(r, 2), GetInt(r, 3)), ct)
            ?? new ReviewStatsRow(0, 0, 0, 0);

        // 4) Aging Bucket (Only Op + Review)
        var intraPendingBucket = await QueryListAsync(conn, SqlQueries.IntraPendingAgingBucketCount, 
            r => new PendingBucketRow(GetInt(r, 0), GetInt(r, 1), GetInt(r, 2), GetInt(r, 3)), ct);
        var interPendingBucket = await QueryListAsync(conn, SqlQueries.InterPendingAgingBucketCount, 
            r => new PendingBucketRow(GetInt(r, 0), GetInt(r, 1), GetInt(r, 2), GetInt(r, 3)), ct);

        // 5) Failures (Rejected, Tech, Compensated)
        var intraFailures = await QuerySingleAsync(conn, SqlQueries.IntraFailures24h, 
            r => new FailuresRow(GetInt(r, 0), GetInt(r, 1), GetInt(r, 2)), ct)
            ?? new FailuresRow(0, 0, 0);
        var interFailures = await QuerySingleAsync(conn, SqlQueries.InterFailures24h, 
            r => new FailuresRow(GetInt(r, 0), GetInt(r, 1), GetInt(r, 2)), ct)
            ?? new FailuresRow(0, 0, 0);

        // 6) Type Count 24h
        var intraTypeCount = await QueryListAsync(conn, SqlQueries.IntraTypeCount24h, 
            r => new TypeCountRow(GetInt(r, 0), GetInt(r, 1)), ct);
        var interTypeCount = await QueryListAsync(conn, SqlQueries.InterTypeCount24h, 
            r => new TypeCountRow(GetInt(r, 0), GetInt(r, 1)), ct);

        // 7) Amount Type x Moneda 24h
        var intraAmountByType = await QueryListAsync(conn, SqlQueries.IntraAmountByType24h, 
            r => new AmountByTypeRow(GetInt(r, 0), GetInt(r, 1), GetDouble(r, 2)), ct);
        var interAmountByType = await QueryListAsync(conn, SqlQueries.InterAmountByType24h, 
            r => new AmountByTypeRow(GetInt(r, 0), GetInt(r, 1), GetDouble(r, 2)), ct);

        // 8) Success Speed proxy 24h (Avg, P95)
        var intraSuccessSpeed = await QueryListAsync(conn, SqlQueries.IntraSuccessSpeed24h, 
            r => new SpeedStatsRow(GetInt(r, 0), GetInt(r, 1), Convert.ToInt32(r.GetValue(2))), ct);
        var interSuccessSpeed = await QueryListAsync(conn, SqlQueries.InterSuccessSpeed24h, 
            r => new SpeedStatsRow(GetInt(r, 0), GetInt(r, 1), Convert.ToInt32(r.GetValue(2))), ct);

        // 9) Interbank specifics
        var interBankCount = await QueryListAsync(conn, SqlQueries.InterBankCount24h, 
            r => new BankCountRow(GetInt(r, 0), GetInt(r, 1)), ct);
        var interBankStateCount = await QueryListAsync(conn, SqlQueries.InterBankStateCount24h, 
            r => new BankStateCountRow(GetInt(r, 0), GetInt(r, 1), GetInt(r, 2)), ct);
        var interBankAmountTotal = await QueryListAsync(conn, SqlQueries.InterBankAmountTotal24h, 
            r => new BankAmountRow(GetInt(r, 0), GetInt(r, 1), GetDouble(r, 2)), ct);

        // 10) High Res & Anomalies
        var intraTxCreated5m = await ScalarIntAsync(conn, SqlQueries.IntraTxCreated5m, ct);
        var interTxCreated5m = await ScalarIntAsync(conn, SqlQueries.InterTxCreated5m, ct);
        var intraTxCreated1h = await ScalarIntAsync(conn, SqlQueries.IntraTxCreated1h, ct);
        var interTxCreated1h = await ScalarIntAsync(conn, SqlQueries.InterTxCreated1h, ct);
        
        var intraSuccessSpeedP99 = await ScalarIntAsync(conn, SqlQueries.IntraSuccessSpeedP99_24h, ct);
        var interSuccessSpeedP99 = await ScalarIntAsync(conn, SqlQueries.InterSuccessSpeedP99_24h, ct);
        
        var intraZeroDurationCount = await ScalarIntAsync(conn, SqlQueries.IntraZeroDurationCount24h, ct);
        var interZeroDurationCount = await ScalarIntAsync(conn, SqlQueries.InterZeroDurationCount24h, ct);
        var intraMissingModCount = await ScalarIntAsync(conn, SqlQueries.IntraMissingModificationCount24h, ct);
        var interMissingModCount = await ScalarIntAsync(conn, SqlQueries.InterMissingModificationCount24h, ct);

        // =========================
        // HEALTH & RESOURCES (Comentados por falta de permisos VIEW SERVER STATE)
        // =========================
        var serverSessions = Array.Empty<SessionStatRow>();
        var fileIoStats = Array.Empty<IoStatRow>();
        var databaseSizes = Array.Empty<DatabaseSizeRow>();

        /* 
        serverSessions = await QueryListAsync(conn, SqlQueries.ServerSessions, 
            r => new SessionStatRow(GetString(r, 0), GetString(r, 1), GetInt(r, 2)), ct);
        
        fileIoStats = await QueryListAsync(conn, SqlQueries.FileIoStats, 
            r => new IoStatRow(GetInt(r, 0), GetLong(r, 1), GetLong(r, 2), GetLong(r, 3), GetLong(r, 4)), ct);

        databaseSizes = await QueryListAsync(conn, SqlQueries.DatabaseSize, 
            r => new DatabaseSizeRow(GetString(r, 0), GetInt(r, 1), GetInt(r, 2)), ct);
        */

        var qualityAnomalies = await QueryListAsync(conn, SqlQueries.QualityAnomalies, 
            r => new AnomalyRow(GetString(r, 0), GetInt(r, 1), GetInt(r, 2), GetInt(r, 3), GetInt(r, 4), GetInt(r, 5)), ct);

        // Note: AmountTotal1h, Total24h and other amount scalars are DEPRECATED 
        // to avoid single scalar queries; use tabular amount queries instead (intraAmountByType).
        // Returning 0 for now to maintain snapshot record signature if not removed yet.
        var intraAmountTotal1h = 0.0;
        var interAmountTotal1h = 0.0;

        return new Snapshot(
            dayType,

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
            intraOpPending, interOpPending,
            intraProgrammed, interProgrammed,
            intraReview, interReview,
            intraPendingBucket, interPendingBucket,
            intraFailures, interFailures,
            intraTypeCount, interTypeCount,
            new List<AmountTotalRow>(), new List<AmountTotalRow>(), 
            intraAmountByType, interAmountByType,
            intraSuccessSpeed, interSuccessSpeed,
            
            interBankCount, interBankStateCount, interBankAmountTotal,

            serverSessions,
            fileIoStats,
            databaseSizes,
            qualityAnomalies,

            // New High-Res & Anomalies
            intraTxCreated5m, interTxCreated5m,
            intraTxCreated1h, interTxCreated1h,
            intraAmountTotal1h, interAmountTotal1h,
            intraSuccessSpeedP99, interSuccessSpeedP99,
            intraZeroDurationCount, interZeroDurationCount,
            intraMissingModCount, interMissingModCount
        );
    }

    private static async Task<double> ScalarDoubleAsync(SqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 30 };
        var obj = await cmd.ExecuteScalarAsync(ct);
        return obj == null || obj == DBNull.Value ? 0 : Convert.ToDouble(obj);
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

    private static int GetInt(SqlDataReader r, int i) => r.IsDBNull(i) ? 0 : r.GetInt32(i);
    private static long GetLong(SqlDataReader r, int i) => r.IsDBNull(i) ? 0 : Convert.ToInt64(r.GetValue(i));
    private static double GetDouble(SqlDataReader r, int i) => r.IsDBNull(i) ? 0 : Convert.ToDouble(r.GetValue(i));
    private static string GetString(SqlDataReader r, int i) => r.IsDBNull(i) ? string.Empty : r.GetString(i);

    // DTOs Tabulares
    public sealed record StateCountRow(int Estado, int Count);
    public sealed record PendingBucketRow(int Estado, int Ge14400s, int Ge3600s, int Ge900s);
    public sealed record AgeStatsRow(int Estado, int AvgSec, int MaxSec);
    public sealed record FailuresRow(int Rejected, int FailedTechnical, int Compensated);
    public sealed record ReviewStatsRow(int TotalCount, int AvgSec, int MaxSec, int DeadCount);
    public sealed record TypeCountRow(int Tipo, int Count);
    public sealed record AmountTotalRow(int Moneda, double Total);
    public sealed record AmountByTypeRow(int Tipo, int Moneda, double Total);
    public sealed record SpeedStatsRow(int Tipo, int AvgSec, int P95Sec);
    public sealed record BankCountRow(int Banco, int Count);
    public sealed record BankStateCountRow(int Banco, int Estado, int Count);
    public sealed record BankAmountRow(int Banco, int Moneda, double Total);

    // Health & Anomaly DTOs
    public sealed record SessionStatRow(string Status, string WaitType, int Count);
    public sealed record IoStatRow(int FileId, long ReadStallMs, long WriteStallMs, long BytesRead, long BytesWritten);
    public sealed record DatabaseSizeRow(string FileName, int SizeMB, int UsedMB);
    public sealed record AnomalyRow(string Source, int Tipo, int Estado, int MissingMod, int ZeroDur, int NegDur);

    public sealed record Snapshot(
        // Context
        string DayType,

        // Historical Scalars (Kept for baseline/compatibility)
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

        // Tabular & Operational (Refactored)
        IReadOnlyList<StateCountRow> IntraStateCount24h, IReadOnlyList<StateCountRow> InterStateCount24h,
        IReadOnlyList<StateCountRow> IntraOpPending, IReadOnlyList<StateCountRow> InterOpPending,
        IReadOnlyList<StateCountRow> IntraProgrammed, IReadOnlyList<StateCountRow> InterProgrammed,
        ReviewStatsRow IntraReview, ReviewStatsRow InterReview,
        IReadOnlyList<PendingBucketRow> IntraPendingBucket, IReadOnlyList<PendingBucketRow> InterPendingBucket,
        FailuresRow IntraFailures, FailuresRow InterFailures,
        IReadOnlyList<TypeCountRow> IntraTypeCount, IReadOnlyList<TypeCountRow> InterTypeCount,
        IReadOnlyList<AmountTotalRow> IntraAmountTotal, IReadOnlyList<AmountTotalRow> InterAmountTotal,
        IReadOnlyList<AmountByTypeRow> IntraAmountByType, IReadOnlyList<AmountByTypeRow> InterAmountByType,
        IReadOnlyList<SpeedStatsRow> IntraSuccessSpeed, IReadOnlyList<SpeedStatsRow> InterSuccessSpeed,
        
        // Interbank Details
        IReadOnlyList<BankCountRow> InterBankCount,
        IReadOnlyList<BankStateCountRow> InterBankStateCount,
        IReadOnlyList<BankAmountRow> InterBankAmountTotal,

        // Health & Infrastructure
        IReadOnlyList<SessionStatRow> ServerSessions,
        IReadOnlyList<IoStatRow> FileIoStats,
        IReadOnlyList<DatabaseSizeRow> DatabaseSizes,
        IReadOnlyList<AnomalyRow> QualityAnomalies,

        // High Resolution & Anomalies
        int IntraTxCreated5m, int InterTxCreated5m,
        int IntraTxCreated1h, int InterTxCreated1h,
        double IntraAmountTotal1h, double InterAmountTotal1h,
        int IntraSuccessSpeedP99, int InterSuccessSpeedP99,
        int IntraZeroDurationCount, int InterZeroDurationCount,
        int IntraMissingModCount, int InterMissingModCount
    );
}