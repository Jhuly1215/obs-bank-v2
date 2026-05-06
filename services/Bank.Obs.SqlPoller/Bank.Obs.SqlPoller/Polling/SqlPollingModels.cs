using System.Collections.Generic;

namespace Bank.Obs.SqlPoller.Polling;

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

public sealed record IntraSnapshot(
    int TxCreated5m, int TxCreated15m, int TxCreated1h, int TxCreated24h, int TxCreated7d, int TxCreated30d,
    int PendingCount24h, int PendingCount7d, int PendingOldestSec,
    int ErrorCount24h, int ResolvedCount24h, int ResAvgSec,
    IReadOnlyList<StateCountRow> StateCount24h, IReadOnlyList<StateCountRow> OpPending, IReadOnlyList<StateCountRow> Programmed,
    ReviewStatsRow Review, IReadOnlyList<PendingBucketRow> PendingBucket, FailuresRow Failures,
    IReadOnlyList<TypeCountRow> TypeCount, IReadOnlyList<AmountTotalRow> AmountTotal, IReadOnlyList<AmountByTypeRow> AmountByType,
    IReadOnlyList<SpeedStatsRow> SuccessSpeed,
    int SuccessCount24h, IReadOnlyList<AgeStatsRow> PendingAgeStats, double AmountTotal1h, int SuccessSpeedP99,
    int ZeroDurationCount, int MissingModCount, int ClosedCount24h, int OtherStateCount24h, int CompensatedCurrentCount
);

public sealed record InterbankSnapshot(
    int TxCreated5m, int TxCreated15m, int TxCreated1h, int TxCreated24h, int TxCreated7d, int TxCreated30d,
    int PendingCount24h, int PendingCount7d, int PendingOldestSec,
    int ErrorCount24h, int ResolvedCount24h, int ResAvgSec,
    IReadOnlyList<StateCountRow> StateCount24h, IReadOnlyList<StateCountRow> OpPending, IReadOnlyList<StateCountRow> Programmed,
    ReviewStatsRow Review, IReadOnlyList<PendingBucketRow> PendingBucket, FailuresRow Failures,
    IReadOnlyList<TypeCountRow> TypeCount, IReadOnlyList<AmountTotalRow> AmountTotal, IReadOnlyList<AmountByTypeRow> AmountByType,
    IReadOnlyList<SpeedStatsRow> SuccessSpeed,
    IReadOnlyList<BankCountRow> BankCount, IReadOnlyList<BankStateCountRow> BankStateCount, IReadOnlyList<BankAmountRow> BankAmountTotal,
    int SuccessCount24h, IReadOnlyList<AgeStatsRow> PendingAgeStats, double AmountTotal1h, int SuccessSpeedP99,
    int ZeroDurationCount, int MissingModCount, int ClosedCount24h, int OtherStateCount24h, int CompensatedCurrentCount
);

public sealed record SystemSnapshot(
    string DayType,
    IReadOnlyList<SessionStatRow> ServerSessions,
    IReadOnlyList<IoStatRow> FileIoStats,
    IReadOnlyList<DatabaseSizeRow> DatabaseSizes,
    IReadOnlyList<AnomalyRow> QualityAnomalies
);

public sealed record Snapshot(
    string DayType,
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
    IReadOnlyList<BankCountRow> InterBankCount,
    IReadOnlyList<BankStateCountRow> InterBankStateCount,
    IReadOnlyList<BankAmountRow> InterBankAmountTotal,
    IReadOnlyList<SessionStatRow> ServerSessions,
    IReadOnlyList<IoStatRow> FileIoStats,
    IReadOnlyList<DatabaseSizeRow> DatabaseSizes,
    IReadOnlyList<AnomalyRow> QualityAnomalies,
    int IntraSuccessCount24h, int InterSuccessCount24h,
    IReadOnlyList<AgeStatsRow> IntraPendingAgeStats, IReadOnlyList<AgeStatsRow> InterPendingAgeStats,
    int IntraTxCreated5m, int InterTxCreated5m,
    int IntraTxCreated1h, int InterTxCreated1h,
    double IntraAmountTotal1h, double InterAmountTotal1h,
    int IntraSuccessSpeedP99, int InterSuccessSpeedP99,
    int IntraZeroDurationCount, int InterZeroDurationCount,
    int IntraMissingModCount, int InterMissingModCount,
    int IntraClosedCount24h, int InterClosedCount24h,
    int IntraOtherStateCount24h, int InterOtherStateCount24h,
    int IntraCompensatedCurrentCount, int InterCompensatedCurrentCount
);

