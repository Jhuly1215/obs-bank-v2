using System.Threading;
using Bank.Obs.SqlPoller.Polling;

namespace Bank.Obs.SqlPoller.State;

public sealed class MetricState
{
    private IntraSnapshot _intra;
    private InterbankSnapshot _inter;
    private SystemSnapshot _system;

    public void UpdateIntra(IntraSnapshot s) => Interlocked.Exchange(ref _intra, s);
    public void UpdateInter(InterbankSnapshot s) => Interlocked.Exchange(ref _inter, s);
    public void UpdateSystem(SystemSnapshot s) => Interlocked.Exchange(ref _system, s);

    public IntraSnapshot CurrentIntra => Volatile.Read(ref _intra);
    public InterbankSnapshot CurrentInter => Volatile.Read(ref _inter);
    public SystemSnapshot CurrentSystem => Volatile.Read(ref _system);

    public Snapshot Current
    {
        get
        {
            var intra = Volatile.Read(ref _intra);
            var inter = Volatile.Read(ref _inter);
            var sys = Volatile.Read(ref _system);

            if (intra == null && inter == null && sys == null) return null;

            return new Snapshot(
                sys?.DayType ?? "habil",
                intra?.TxCreated15m ?? 0, inter?.TxCreated15m ?? 0,
                intra?.TxCreated24h ?? 0, inter?.TxCreated24h ?? 0,
                intra?.TxCreated7d ?? 0, inter?.TxCreated7d ?? 0,
                intra?.TxCreated30d ?? 0, inter?.TxCreated30d ?? 0,
                intra?.PendingCount24h ?? 0, inter?.PendingCount24h ?? 0,
                intra?.PendingCount7d ?? 0, inter?.PendingCount7d ?? 0,
                intra?.PendingOldestSec ?? 0, inter?.PendingOldestSec ?? 0,
                intra?.ErrorCount24h ?? 0, inter?.ErrorCount24h ?? 0,
                intra?.ResolvedCount24h ?? 0, inter?.ResolvedCount24h ?? 0,
                intra?.ResAvgSec ?? 0, inter?.ResAvgSec ?? 0,
                intra?.StateCount24h, inter?.StateCount24h,
                intra?.OpPending, inter?.OpPending,
                intra?.Programmed, inter?.Programmed,
                intra?.Review, inter?.Review,
                intra?.PendingBucket, inter?.PendingBucket,
                intra?.Failures, inter?.Failures,
                intra?.TypeCount, inter?.TypeCount,
                intra?.AmountTotal, inter?.AmountTotal,
                intra?.AmountByType, inter?.AmountByType,
                intra?.SuccessSpeed, inter?.SuccessSpeed,
                inter?.BankCount,
                inter?.BankStateCount,
                inter?.BankAmountTotal,
                sys?.ServerSessions,
                sys?.FileIoStats,
                sys?.DatabaseSizes,
                sys?.QualityAnomalies,
                intra?.SuccessCount24h ?? 0, inter?.SuccessCount24h ?? 0,
                intra?.PendingAgeStats, inter?.PendingAgeStats,
                intra?.TxCreated5m ?? 0, inter?.TxCreated5m ?? 0,
                intra?.TxCreated1h ?? 0, inter?.TxCreated1h ?? 0,
                intra?.AmountTotal1h ?? 0, inter?.AmountTotal1h ?? 0,
                intra?.SuccessSpeedP99 ?? 0, inter?.SuccessSpeedP99 ?? 0,
                intra?.ZeroDurationCount ?? 0, inter?.ZeroDurationCount ?? 0,
                intra?.MissingModCount ?? 0, inter?.MissingModCount ?? 0,
                intra?.ClosedCount24h ?? 0, inter?.ClosedCount24h ?? 0,
                intra?.OtherStateCount24h ?? 0, inter?.OtherStateCount24h ?? 0,
                intra?.CompensatedCurrentCount ?? 0, inter?.CompensatedCurrentCount ?? 0
            );
        }
    }
}
