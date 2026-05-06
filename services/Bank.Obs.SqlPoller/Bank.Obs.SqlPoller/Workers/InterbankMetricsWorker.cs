using Bank.Obs.SqlPoller.Metrics;
using Bank.Obs.SqlPoller.Polling;
using Bank.Obs.SqlPoller.State;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Bank.Obs.SqlPoller.Workers;

public sealed class InterbankMetricsWorker : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<InterbankMetricsWorker> _logger;
    private readonly MetricState _state;
    private readonly InterbankPollingService _poller;
    private readonly SqlMetrics _metrics;

    public InterbankMetricsWorker(
        IConfiguration config,
        ILogger<InterbankMetricsWorker> logger,
        MetricState state,
        InterbankPollingService poller,
        SqlMetrics metrics)
    {
        _config = config;
        _logger = logger;
        _state = state;
        _poller = poller;
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connString = _config["SqlPoller:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connString))
            throw new InvalidOperationException("Falta SqlPoller:ConnectionString");

        var intervalSeconds = int.TryParse(_config["SqlPoller:IntervalSeconds"], out var s) ? s : 60;

        while (!stoppingToken.IsCancellationRequested)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var snap = await _poller.PollAsync(connString, stoppingToken);

                _state.UpdateInter(snap);

                sw.Stop();
                // We don't record success metrics here to avoid double counting, or we could have specific Interbank poll duration metrics. 
                // For now we just log it.

                _logger.LogInformation(
                    "Interbank poll ok. inter(15m={e15}, pend24={ep24}, err24={ee24}, oldSec={eo}) duration_ms={durationMs}",
                    snap.TxCreated15m, snap.PendingCount24h, snap.ErrorCount24h, snap.PendingOldestSec,
                    sw.ElapsedMilliseconds);

            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Error en Interbank poller");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
