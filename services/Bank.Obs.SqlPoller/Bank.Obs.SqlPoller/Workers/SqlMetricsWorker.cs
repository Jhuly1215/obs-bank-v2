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
            var sw = Stopwatch.StartNew();

            try
            {
                var snap = await _poller.PollAsync(connString, stoppingToken);

                // Actualiza todo con 1 lock
                _state.Update(snap);

                sw.Stop();
                _metrics.RecordPollSuccess(sw.Elapsed.TotalSeconds);

                _logger.LogInformation(
                    "SQL poll base ok. intra(15m={i15}, pend24={ip24}, err24={ie24}, oldSec={io}) inter(15m={e15}, pend24={ep24}, err24={ee24}, oldSec={eo}) duration_ms={durationMs}",
                    snap.IntraTxCreated15m, snap.IntraPendingCount24h, snap.IntraErrorCount24h, snap.IntraPendingOldestSec,
                    snap.InterTxCreated15m, snap.InterPendingCount24h, snap.InterErrorCount24h, snap.InterPendingOldestSec,
                    sw.ElapsedMilliseconds);

                // Agregar un Warning simulado o condicional para efectos de prueba de Observabilidad
                if (snap.IntraPendingCount24h > 100 || snap.InterPendingCount24h > 100)
                {
                    _logger.LogWarning("Nivel de transacciones pendientes elevado en las últimas 24h. Intra: {IntraPending}, Inter: {InterPending}", snap.IntraPendingCount24h, snap.InterPendingCount24h);
                }
                else 
                {
                    // Simulando una advertencia ocasional
                    if (DateTime.UtcNow.Minute % 5 == 0)
                    {
                        _logger.LogWarning("Revisión periódica de rendimiento (cada 5 min). La conexión a la BD está operando normalmente.");
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _metrics.RecordPollError(sw.Elapsed.TotalSeconds);

                _logger.LogError(ex, "Error en SQL poller");
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