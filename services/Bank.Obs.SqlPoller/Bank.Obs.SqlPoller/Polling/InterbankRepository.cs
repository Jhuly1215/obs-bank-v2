using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Bank.Obs.SqlPoller.Polling;

public sealed class InterbankRepository
{
    private readonly ISqlExecutor _executor;

    public InterbankRepository(ISqlExecutor executor)
    {
        _executor = executor;
    }

    public async Task<(
        IReadOnlyList<BankCountRow> InterBankCount,
        IReadOnlyList<BankStateCountRow> InterBankStateCount,
        IReadOnlyList<BankAmountRow> InterBankAmountTotal
    )> GetMetricsAsync(SqlConnection conn, CancellationToken ct)
    {
        var interBankCount = await _executor.QueryListAsync(conn, SqlQueries.InterBankCount24h, 
            r => new BankCountRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1)), ct);
        
        var interBankStateCount = await _executor.QueryListAsync(conn, SqlQueries.InterBankStateCount24h, 
            r => new BankStateCountRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetInt(r, 2)), ct);
        
        var interBankAmountTotal = await _executor.QueryListAsync(conn, SqlQueries.InterBankAmountTotal24h, 
            r => new BankAmountRow(SqlReaderHelper.GetInt(r, 0), SqlReaderHelper.GetInt(r, 1), SqlReaderHelper.GetDouble(r, 2)), ct);

        return (interBankCount, interBankStateCount, interBankAmountTotal);
    }
}
