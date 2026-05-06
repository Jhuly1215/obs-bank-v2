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

public sealed class IntraMetricsWorker : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<IntraMetricsWorker> _logger;
    private readonly MetricState _state;
    private readonly IntraPollingService _poller;
    private readonly SqlMetrics _metrics;

    public IntraMetricsWorker(
        IConfiguration config,
        ILogger<IntraMetricsWorker> logger,
        MetricState state,
        IntraPollingService poller,
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

                _state.UpdateIntra(snap);

                sw.Stop();
                _metrics.RecordPollSuccess(sw.Elapsed.TotalSeconds);

                _logger.LogInformation(
                    "Intra poll ok. intra(15m={i15}, pend24={ip24}, err24={ie24}, oldSec={io}) duration_ms={durationMs}",
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
                _metrics.RecordPollError(sw.Elapsed.TotalSeconds);
                _logger.LogError(ex, "Error en Intra poller");
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
