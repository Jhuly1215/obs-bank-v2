using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Bank.Obs.DemoApi.Services;

[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ILogger<TransactionsController> _log;
    private readonly IdempotencyStore _idemp;

    public TransactionsController(ILogger<TransactionsController> log, IdempotencyStore idemp)
    {
        _log = log;
        _idemp = idemp;
    }

    [HttpPost("transfer")]
    [Authorize(Policy = "CanTransfer")]
    [EnableRateLimiting("transfer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Transfer(
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        [FromBody] TransferRequest req,
        CancellationToken ct)
     {

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { error = "Idempotency-Key header required" });
        if (_idemp.TryGet(idempotencyKey, out var existing))
        {
            // Return previous result (demonstration)
            _log.LogInformation("Idempotency hit for key {Key}", idempotencyKey);
            return Ok(new { transactionId = existing.TransactionId, status = existing.Status });
        }

        var txId = Guid.NewGuid().ToString("N");

        // Simulate processing with cancellation support
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), ct);

            var outcome = "success";
            _log.LogInformation("Processed transfer tx={Tx} outcome={Outcome}", txId, outcome);

            _idemp.Save(idempotencyKey, new IdempotencyRecord(idempotencyKey, txId, outcome, "", DateTime.UtcNow));
            return Ok(new { transactionId = txId, status = outcome });
        }
        catch (OperationCanceledException)
        {
            _log.LogWarning("Transfer cancelled tx={Tx}", txId);
            return StatusCode(499); // client closed request
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error processing transfer tx={Tx}", txId);
            return StatusCode(500, new { error = "internal_error", traceId = HttpContext.TraceIdentifier });
        }
    }
}