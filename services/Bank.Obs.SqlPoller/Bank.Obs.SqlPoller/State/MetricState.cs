using System.Threading;
using Bank.Obs.SqlPoller.Polling;

namespace Bank.Obs.SqlPoller.State;

public sealed class MetricState
{
    private SqlPollingClient.Snapshot _currentSnapshot;

    public void Update(SqlPollingClient.Snapshot s)
    {
        Interlocked.Exchange(ref _currentSnapshot, s);
    }

    public SqlPollingClient.Snapshot Current => Volatile.Read(ref _currentSnapshot);
}