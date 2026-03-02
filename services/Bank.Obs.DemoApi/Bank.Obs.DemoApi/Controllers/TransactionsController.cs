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

    private readonly IIdempotencyStore _idemp;
    private readonly TimeSpan _idempTtl;

    public TransactionsController(
        ILogger<TransactionsController> log,
        IIdempotencyStore idemp,
        IdempotencyTtl ttl)
    {
        _log = log;
        _idemp = idemp;
        _idempTtl = ttl.Value;
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

  
        var existing = await _idemp.GetAsync(idempotencyKey, ct);
        if (existing is not null && existing.Status != "pending")
        {
            _log.LogInformation("Idempotency hit for key {Key}", idempotencyKey);
            return Ok(new { transactionId = existing.TransactionId, status = existing.Status });
        }

        var reserved = await _idemp.TryReserveAsync(idempotencyKey, _idempTtl, ct);
        if (!reserved)
        {
            var now = await _idemp.GetAsync(idempotencyKey, ct);
            if (now is not null)
                return Ok(new { transactionId = now.TransactionId, status = now.Status });
            return Conflict(new { error = "duplicate_request", message = "Request is already being processed" });
        }

        var txId = Guid.NewGuid().ToString("N");

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), ct);

            var outcome = "success";
            _log.LogInformation("Processed transfer tx={Tx} outcome={Outcome}", txId, outcome);

            var record = new IdempotencyRecord(idempotencyKey, txId, outcome, "", DateTime.UtcNow);
            await _idemp.SetAsync(idempotencyKey, record, _idempTtl, ct);

            return Ok(new { transactionId = txId, status = outcome });
        }
        catch (OperationCanceledException)
        {
            _log.LogWarning("Transfer cancelled tx={Tx}", txId);
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error processing transfer tx={Tx}", txId);
            var record = new IdempotencyRecord(idempotencyKey, txId, "error", "", DateTime.UtcNow);
            await _idemp.SetAsync(idempotencyKey, record, _idempTtl, ct);

            return StatusCode(500, new { error = "internal_error", traceId = HttpContext.TraceIdentifier });
        }
    }
}