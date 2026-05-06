using Microsoft.Data.SqlClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bank.Obs.SqlPoller.Polling;

public sealed class SystemPollingService
{
    private readonly FailureRepository _failure;

    public SystemPollingService(FailureRepository failure)
    {
        _failure = failure;
    }

    public async Task<SystemSnapshot> PollAsync(string connString, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connString);
        await conn.OpenAsync(ct);

        var dayTypeObj = await new SqlCommand(SqlQueries.GetDayType, conn).ExecuteScalarAsync(ct);
        var dayType = dayTypeObj?.ToString() ?? "habil";

        var anomalies = await _failure.GetSystemMetricsAsync(conn, ct);

        var serverSessions = Array.Empty<SessionStatRow>();
        var fileIoStats = Array.Empty<IoStatRow>();
        var databaseSizes = Array.Empty<DatabaseSizeRow>();

        return new SystemSnapshot(
            dayType,
            serverSessions,
            fileIoStats,
            databaseSizes,
            anomalies
        );
    }
}
