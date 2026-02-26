using Bank.Obs.SqlPoller.State;
using System;
using System.Diagnostics.Metrics;
using System.Threading;

namespace Bank.Obs.SqlPoller.Metrics;

public sealed class SqlMetrics : IDisposable
{
    public const string MeterName = "bank.sql.metrics";

    private readonly Meter _meter;

    // Métricas operativas del poller
    private readonly Counter<long> _pollSuccessTotal;
    private readonly Counter<long> _pollErrorTotal;
    private readonly Histogram<double> _pollDurationSeconds;

    private long _lastPollUnixTime;
    private long _lastSuccessUnixTime;

    public SqlMetrics(MetricState state)
    {
        _meter = new Meter(MeterName, "1.0.0");

        // =========================
        // Negocio (snapshots SQL)
        // =========================
        _meter.CreateObservableGauge("bank_intra_tx_30d_total", () => state.IntraTxLast30d, "tx",
            "Total transferencias intra observadas últimos 30d");

        _meter.CreateObservableGauge("bank_inter_tx_30d_total", () => state.InterTxLast30d, "tx",
            "Total transferencias inter observadas últimos 30d");

        _meter.CreateObservableGauge("bank_intra_pending_7d_total", () => state.IntraPendingLast7d, "tx",
            "Pendientes intra (proxy) últimos 7d");

        _meter.CreateObservableGauge("bank_inter_pending_7d_total", () => state.InterPendingLast7d, "tx",
            "Pendientes inter (proxy) últimos 7d");

        _meter.CreateObservableGauge("bank_intra_fail_tech_rate_30d", () => state.IntraFailTechRate30d, "ratio",
            "Tasa de falla técnica intra últimos 30d");

        _meter.CreateObservableGauge("bank_inter_fail_tech_rate_30d", () => state.InterFailTechRate30d, "ratio",
            "Tasa de falla técnica inter últimos 30d");

        _meter.CreateObservableGauge("bank_inter_state9_observed_share_30d", () => state.IntraState9InterShare30d, "ratio",
            "Proporción observada de estado=9 en interbancarias (30d)");

        _meter.CreateObservableGauge("bank_intra_pending_max_age_min_7d", () => state.IntraPendingMaxAgeMin, "min",
            "Edad máxima de pendientes intra (proxy) en 7d");

        _meter.CreateObservableGauge("bank_inter_pending_max_age_min_7d", () => state.InterPendingMaxAgeMin, "min",
            "Edad máxima de pendientes inter (proxy) en 7d");

        // =========================
        // Operativas del poller
        // =========================
        _pollSuccessTotal = _meter.CreateCounter<long>(
            "bank_sql_poller_poll_success_total",
            unit: "poll",
            description: "Ciclos de polling SQL exitosos");

        _pollErrorTotal = _meter.CreateCounter<long>(
            "bank_sql_poller_poll_error_total",
            unit: "poll",
            description: "Ciclos de polling SQL con error");

        _pollDurationSeconds = _meter.CreateHistogram<double>(
            "bank_sql_poller_poll_duration_seconds",
            unit: "s",
            description: "Duración del ciclo de polling SQL");

        _meter.CreateObservableGauge("bank_sql_poller_last_poll_unixtime", () => Interlocked.Read(ref _lastPollUnixTime), "s",
            "Unix time del último intento de poll");

        _meter.CreateObservableGauge("bank_sql_poller_last_success_unixtime", () => Interlocked.Read(ref _lastSuccessUnixTime), "s",
            "Unix time del último poll exitoso");
    }

    public void RecordPollSuccess(double durationSeconds)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Interlocked.Exchange(ref _lastPollUnixTime, now);
        Interlocked.Exchange(ref _lastSuccessUnixTime, now);

        _pollSuccessTotal.Add(1);
        _pollDurationSeconds.Record(durationSeconds);
    }

    public void RecordPollError(double durationSeconds)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Interlocked.Exchange(ref _lastPollUnixTime, now);

        _pollErrorTotal.Add(1);
        _pollDurationSeconds.Record(durationSeconds);
    }

    public void Dispose() => _meter.Dispose();
}