using Bank.Obs.SqlPoller.Metrics; 
using Bank.Obs.SqlPoller.Polling;
using Bank.Obs.SqlPoller.State;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bank.Obs.SqlPoller.Workers;

public sealed class SqlMetricsWorker : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SqlMetricsWorker> _logger;
    private readonly MetricState _state;
    private readonly SqlPollingClient _poller;
    private readonly SqlMetrics _metrics;

    public SqlMetricsWorker(
        IConfiguration config,
        ILogger<SqlMetricsWorker> logger,
        MetricState state,
        SqlPollingClient poller,
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
            try
            {
                var snap = await _poller.PollAsync(connString, stoppingToken);

                _state.Update(
                    snap.IntraTxLast30d,
                    snap.InterTxLast30d,
                    snap.IntraPendingLast7d,
                    snap.InterPendingLast7d,
                    snap.IntraFailTechRate30d,
                    snap.InterFailTechRate30d,
                    snap.IntraState9InterShare30d,
                    snap.IntraPendingMaxAgeMin,
                    snap.InterPendingMaxAgeMin);

                _logger.LogInformation(
                    "SQL poll ok. intra30d={intra30d}, inter30d={inter30d}, backlog_intra_7d={bintra}, backlog_inter_7d={binter}",
                    snap.IntraTxLast30d,
                    snap.InterTxLast30d,
                    snap.IntraPendingLast7d,
                    snap.InterPendingLast7d);

                // Si tu clase SqlMetrics tiene métodos imperativos (counter/histogram), llámalos aquí.
                // Ejemplo (solo si existen):
                // _metrics.RecordPollSuccess();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // apagado normal del servicio
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SQL poller");

                // Si tu clase SqlMetrics tiene contador de errores, úsalo aquí.
                // Ejemplo (solo si existe):
                // _metrics.RecordPollError();
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