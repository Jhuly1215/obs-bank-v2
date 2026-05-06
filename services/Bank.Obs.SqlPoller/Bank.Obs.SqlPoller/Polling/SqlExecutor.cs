using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Bank.Obs.SqlPoller.Polling;

public interface ISqlExecutor
{
    Task<double> ScalarDoubleAsync(SqlConnection conn, string sql, CancellationToken ct);
    Task<int> ScalarIntAsync(SqlConnection conn, string sql, CancellationToken ct);
    Task<List<T>> QueryListAsync<T>(SqlConnection conn, string sql, Func<SqlDataReader, T> map, CancellationToken ct);
    Task<T?> QuerySingleAsync<T>(SqlConnection conn, string sql, Func<SqlDataReader, T> map, CancellationToken ct) where T : class;
}

public sealed class SqlExecutor : ISqlExecutor
{
    public async Task<double> ScalarDoubleAsync(SqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 30 };
        var obj = await cmd.ExecuteScalarAsync(ct);
        return obj == null || obj == DBNull.Value ? 0 : Convert.ToDouble(obj);
    }

    public async Task<int> ScalarIntAsync(SqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 30 };
        var obj = await cmd.ExecuteScalarAsync(ct);
        return obj == null || obj == DBNull.Value ? 0 : Convert.ToInt32(obj);
    }

    public async Task<List<T>> QueryListAsync<T>(SqlConnection conn, string sql, Func<SqlDataReader, T> map, CancellationToken ct)
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

    public async Task<T?> QuerySingleAsync<T>(SqlConnection conn, string sql, Func<SqlDataReader, T> map, CancellationToken ct) where T : class
    {
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (await r.ReadAsync(ct)) return map(r);
        return null;
    }
}
