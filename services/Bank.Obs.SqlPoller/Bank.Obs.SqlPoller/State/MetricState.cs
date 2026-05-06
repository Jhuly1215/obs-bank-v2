using System.Threading;
using Bank.Obs.SqlPoller.Polling;

namespace Bank.Obs.SqlPoller.State;

public sealed class MetricState
{
    private Snapshot _currentSnapshot;

    public void Update(Snapshot s)
    {
        Interlocked.Exchange(ref _currentSnapshot, s);
    }

    public Snapshot Current => Volatile.Read(ref _currentSnapshot);
}
