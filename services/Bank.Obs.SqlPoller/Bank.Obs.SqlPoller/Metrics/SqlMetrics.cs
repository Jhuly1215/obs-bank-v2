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
        // Negocio (snapshots SQL) - Volumen
        // =========================

        // 15m (latido)
        _meter.CreateObservableGauge("bank_intra_tx_15m_total", () => state.IntraTxLast15m, "tx",
            "Total transferencias intra observadas últimos 15m");

        _meter.CreateObservableGauge("bank_inter_tx_15m_total", () => state.InterTxLast15m, "tx",
            "Total transferencias inter observadas últimos 15m");

        // 24h
        _meter.CreateObservableGauge("bank_intra_tx_24h_total", () => state.IntraTxLast24h, "tx",
            "Total transferencias intra observadas últimas 24h");

        _meter.CreateObservableGauge("bank_inter_tx_24h_total", () => state.InterTxLast24h, "tx",
            "Total transferencias inter observadas últimas 24h");

        // 30d
        _meter.CreateObservableGauge("bank_intra_tx_30d_total", () => state.IntraTxLast30d, "tx",
            "Total transferencias intra observadas últimos 30d");

        _meter.CreateObservableGauge("bank_inter_tx_30d_total", () => state.InterTxLast30d, "tx",
            "Total transferencias inter observadas últimos 30d");

        // =========================
        // Negocio - Backlog / Pendientes (estado actual)
        // (Regla: estado 9 = error, NO pendiente)
        // =========================

        // 24h
        _meter.CreateObservableGauge("bank_intra_pending_24h_total", () => state.IntraPendingCount24h, "tx",
            "Pendientes intra (snapshot por estado actual) creadas/observadas en 24h");

        _meter.CreateObservableGauge("bank_inter_pending_24h_total", () => state.InterPendingCount24h, "tx",
            "Pendientes inter (snapshot por estado actual) creadas/observadas en 24h");

        // 7d
        _meter.CreateObservableGauge("bank_intra_pending_7d_total", () => state.IntraPendingLast7d, "tx",
            "Pendientes intra (snapshot por estado actual) últimos 7d");

        _meter.CreateObservableGauge("bank_inter_pending_7d_total", () => state.InterPendingLast7d, "tx",
            "Pendientes inter (snapshot por estado actual) últimos 7d");

        // max age (7d)
        _meter.CreateObservableGauge("bank_intra_pending_max_age_min_7d", () => state.IntraPendingMaxAgeMin, "min",
            "Edad máxima de pendientes intra en 7d");

        _meter.CreateObservableGauge("bank_inter_pending_max_age_min_7d", () => state.InterPendingMaxAgeMin, "min",
            "Edad máxima de pendientes inter en 7d");

        // =========================
        // Negocio - Calidad / Errores
        // =========================

        // Intra 30d rates
        _meter.CreateObservableGauge("bank_intra_fail_tech_rate_30d", () => state.IntraFailTechRate30d, "ratio",
            "Tasa de error técnico intra últimos 30d (incluye estado 9)");

        // Inter 30d rates
        _meter.CreateObservableGauge("bank_inter_fail_tech_rate_30d", () => state.InterFailTechRate30d, "ratio",
            "Tasa de error técnico inter últimos 30d (incluye estado 9)");

        // Inter estado 9 (error) share 30d
        _meter.CreateObservableGauge("bank_inter_error_state9_share_30d", () => state.InterErrorState9Share30d, "ratio",
            "Proporción de estado=9 (error) en interbancarias últimos 30d");

        // 24h rates
        _meter.CreateObservableGauge("bank_intra_fail_tech_rate_24h", () => state.IntraFailTechRate24h, "ratio",
            "Tasa de error técnico intra últimas 24h (incluye estado 9)");

        _meter.CreateObservableGauge("bank_inter_fail_tech_rate_24h", () => state.InterFailTechRate24h, "ratio",
            "Tasa de error técnico inter últimas 24h (incluye estado 9)");

        _meter.CreateObservableGauge("bank_inter_error_state9_share_24h", () => state.InterErrorState9Share24h, "ratio",
            "Proporción de estado=9 (error) en interbancarias últimas 24h");

        // Conteos de error 24h (scalars)
        _meter.CreateObservableGauge("bank_intra_error_24h_total", () => state.IntraErrorCount24h, "tx",
            "Total errores intra últimas 24h (estado IN 5,9,15)");

        _meter.CreateObservableGauge("bank_inter_error_24h_total", () => state.InterErrorCount24h, "tx",
            "Total errores inter últimas 24h (estado IN 5,9)");

        // Edad máxima de errores (7d)
        _meter.CreateObservableGauge("bank_intra_error_max_age_min_7d", () => state.IntraErrorMaxAgeMin7d, "min",
            "Edad máxima de errores intra en 7d (estado IN 5,9,15)");

        _meter.CreateObservableGauge("bank_inter_error_max_age_min_7d", () => state.InterErrorMaxAgeMin7d, "min",
            "Edad máxima de errores inter en 7d (estado IN 5,9)");

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