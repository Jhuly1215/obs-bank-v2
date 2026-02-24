using Bank.Obs.DemoApi.Services;
using System.Threading;

namespace Bank.Obs.DemoApi.Endpoints;

public static class TransactionsEndpoints
{
    public static IEndpointRouteBuilder MapTransactions(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/transactions/transfer",
            async (ITransferService service, CancellationToken ct) =>
            {
                var result = await service.SimulateTransferAsync(channel: "api", ct);

                return result.Success
                    ? Results.Ok(new { transactionId = result.TransactionId, outcome = result.Outcome })
                    : Results.Problem("Transfer failed", statusCode: 502, extensions: new Dictionary<string, object?>
                    {
                        ["transactionId"] = result.TransactionId,
                        ["errorCode"] = result.ErrorCode
                    });
            })
            .WithName("Transfer");

        return app;
    }
}