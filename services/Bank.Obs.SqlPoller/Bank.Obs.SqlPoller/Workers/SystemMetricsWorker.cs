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

public sealed class SystemMetricsWorker : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SystemMetricsWorker> _logger;
    private readonly MetricState _state;
    private readonly SystemPollingService _poller;

    public SystemMetricsWorker(
        IConfiguration config,
        ILogger<SystemMetricsWorker> logger,
        MetricState state,
        SystemPollingService poller)
    {
        _config = config;
        _logger = logger;
        _state = state;
        _poller = poller;
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

                _state.UpdateSystem(snap);

                sw.Stop();

                _logger.LogInformation(
                    "System poll ok. dayType={day} anomalies={anomalies} duration_ms={durationMs}",
                    snap.DayType, snap.QualityAnomalies?.Count ?? 0,
                    sw.ElapsedMilliseconds);

            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Error en System poller");
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
