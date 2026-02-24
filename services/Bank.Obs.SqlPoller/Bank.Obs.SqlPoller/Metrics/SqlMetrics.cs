using Bank.Obs.SqlPoller.State;
using System;
using System.Diagnostics.Metrics;

namespace Bank.Obs.SqlPoller.Metrics;

public sealed class SqlMetrics : IDisposable
{
    public const string MeterName = "bank.sql.metrics";

    private readonly Meter _meter;

    public SqlMetrics(MetricState state)
    {
        _meter = new Meter(MeterName, "1.0.0");

        _meter.CreateObservableGauge("bank_intra_tx_30d_total", () => state.IntraTxLast30d, "tx",
            "Total transferencias intra últimos 30d");
        _meter.CreateObservableGauge("bank_inter_tx_30d_total", () => state.InterTxLast30d, "tx",
            "Total transferencias inter últimos 30d");

        _meter.CreateObservableGauge("bank_intra_pending_7d_total", () => state.IntraPendingLast7d, "tx",
            "Backlog activo intra (7d)");
        _meter.CreateObservableGauge("bank_inter_pending_7d_total", () => state.InterPendingLast7d, "tx",
            "Backlog activo inter (7d)");

        _meter.CreateObservableGauge("bank_intra_fail_tech_rate_30d", () => state.IntraFailTechRate30d, "ratio",
            "Tasa fallas técnicas intra (5,15 / 30d)");
        _meter.CreateObservableGauge("bank_inter_fail_tech_rate_30d", () => state.InterFailTechRate30d, "ratio",
            "Tasa fallas técnicas inter (5 / 30d)");

        _meter.CreateObservableGauge("bank_inter_state9_share_30d", () => state.IntraState9InterShare30d, "ratio",
            "Proporción estado 9 en interbancarias (30d)");

        _meter.CreateObservableGauge("bank_intra_pending_max_age_min_7d", () => state.IntraPendingMaxAgeMin, "min",
            "Edad máxima pendientes intra (7d)");
        _meter.CreateObservableGauge("bank_inter_pending_max_age_min_7d", () => state.InterPendingMaxAgeMin, "min",
            "Edad máxima pendientes inter (7d)");
    }

    public void Dispose() => _meter.Dispose();
}