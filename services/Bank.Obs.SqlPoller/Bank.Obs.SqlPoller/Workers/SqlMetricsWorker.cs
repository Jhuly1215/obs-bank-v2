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
                _state.Update(
                    // Volumen
                    snap.IntraTxLast15m,
                    snap.InterTxLast15m,
                    snap.IntraTxLast24h,
                    snap.InterTxLast24h,
                    snap.IntraTxLast30d,
                    snap.InterTxLast30d,

                    // Pendientes
                    snap.IntraPendingCount24h,
                    snap.InterPendingCount24h,
                    snap.IntraPendingLast7d,
                    snap.InterPendingLast7d,
                    snap.IntraPendingMaxAgeMin,
                    snap.InterPendingMaxAgeMin,

                    // Calidad / errores
                    snap.IntraFailTechRate24h,
                    snap.InterFailTechRate24h,
                    snap.IntraFailTechRate30d,
                    snap.InterFailTechRate30d,
                    snap.InterErrorState9Share24h,
                    snap.InterErrorState9Share30d,
                    snap.IntraErrorCount24h,
                    snap.InterErrorCount24h,
                    snap.IntraErrorMaxAgeMin7d,
                    snap.InterErrorMaxAgeMin7d);

                sw.Stop();
                _metrics.RecordPollSuccess(sw.Elapsed.TotalSeconds);

                _logger.LogInformation(
                    "SQL poll ok. 15m(intra={intra15m}, inter={inter15m}) 24h(intra={intra24h}, inter={inter24h}) " +
                    "30d(intra={intra30d}, inter={inter30d}) pending24h(intra={pintra24h}, inter={pinter24h}) " +
                    "pending7d(intra={pintra7d}, inter={pinter7d}) pendingMaxAgeMin7d(intra={pintraAge}, inter={pinterAge}) " +
                    "err24h(intra={eintra24h}, inter={einter24h}) errMaxAgeMin7d(intra={eintraAge}, inter={einterAge}) " +
                    "failRate24h(intra={fintra24h}, inter={finter24h}) failRate30d(intra={fintra30d}, inter={finter30d}) " +
                    "state9Share30d(inter={s9_30d}) duration_ms={durationMs}",
                    snap.IntraTxLast15m,
                    snap.InterTxLast15m,
                    snap.IntraTxLast24h,
                    snap.InterTxLast24h,
                    snap.IntraTxLast30d,
                    snap.InterTxLast30d,
                    snap.IntraPendingCount24h,
                    snap.InterPendingCount24h,
                    snap.IntraPendingLast7d,
                    snap.InterPendingLast7d,
                    snap.IntraPendingMaxAgeMin,
                    snap.InterPendingMaxAgeMin,
                    snap.IntraErrorCount24h,
                    snap.InterErrorCount24h,
                    snap.IntraErrorMaxAgeMin7d,
                    snap.InterErrorMaxAgeMin7d,
                    snap.IntraFailTechRate24h,
                    snap.InterFailTechRate24h,
                    snap.IntraFailTechRate30d,
                    snap.InterFailTechRate30d,
                    snap.InterErrorState9Share30d,
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