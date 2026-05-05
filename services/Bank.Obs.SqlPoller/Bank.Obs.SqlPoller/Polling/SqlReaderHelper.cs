using Microsoft.Data.SqlClient;
using System;

namespace Bank.Obs.SqlPoller.Polling;

public static class SqlReaderHelper
{
    public static int GetInt(SqlDataReader r, int i) => r.IsDBNull(i) ? 0 : r.GetInt32(i);
    public static long GetLong(SqlDataReader r, int i) => r.IsDBNull(i) ? 0 : Convert.ToInt64(r.GetValue(i));
    public static double GetDouble(SqlDataReader r, int i) => r.IsDBNull(i) ? 0 : Convert.ToDouble(r.GetValue(i));
    public static string GetString(SqlDataReader r, int i) => r.IsDBNull(i) ? string.Empty : r.GetString(i);
}
