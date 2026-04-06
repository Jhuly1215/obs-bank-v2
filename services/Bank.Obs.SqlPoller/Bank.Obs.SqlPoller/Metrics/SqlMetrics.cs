using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using Bank.Obs.SqlPoller.Polling;
using Bank.Obs.SqlPoller.State;

namespace Bank.Obs.SqlPoller.Metrics;

public sealed class SqlMetrics : System.IDisposable
{
    // Meter
    public const string MeterName = "bank-sql-poller";
    private readonly Meter _meter;
    private readonly MetricState _state;

    // Poller Health
    private readonly Counter<long> _pollErrorTotal;
    private readonly Histogram<double> _pollDurationSeconds;
    private long _lastPollUnixTime;
    private long _consecutiveFailures;

    public SqlMetrics(MetricState state)
    {
        _state = state;
        _meter = new Meter(MeterName, "2.0.0");

        // Health
        _pollErrorTotal = _meter.CreateCounter<long>("sql_poller_errors_total", description: "Cantidad de ciclos de polling fallidos");
        _pollDurationSeconds = _meter.CreateHistogram<double>("sql_poller_cycle_duration_seconds", unit: "s", description: "Duración de los ciclos de consulta a BD en segundos");
        _meter.CreateObservableGauge("sql_poller_last_success_timestamp", () => Volatile.Read(ref _lastPollUnixTime), unit: "s", description: "Timestamp del último poll exitoso");
        _meter.CreateObservableGauge("sql_poller_consecutive_failures", () => Volatile.Read(ref _consecutiveFailures), description: "Ciclos consecutivos fallidos");

        // =========================
        // Historical Scalars (Kept for compatibility)
        // =========================
        _meter.CreateObservableGauge("tx_created_total_15m", CreateVolObs, description: "TX Creadas 15m");
        _meter.CreateObservableGauge("tx_created_total_24h", CreateVolObs24, description: "TX Creadas 24h");
        _meter.CreateObservableGauge("tx_created_total_7d", CreateVolObs7, description: "TX Creadas 7d");
        _meter.CreateObservableGauge("tx_created_total_30d", CreateVolObs30, description: "TX Creadas 30d");

        _meter.CreateObservableGauge("tx_pending_count_24h", CreatePend24, description: "TX Pendientes 24h");
        _meter.CreateObservableGauge("tx_pending_count_7d", CreatePend7, description: "TX Pendientes 7d");
        _meter.CreateObservableGauge("tx_pending_oldest_seconds", CreatePendOldest, unit: "s", description: "Antigüedad en segundos de la más vieja");

        _meter.CreateObservableGauge("tx_error_count_24h", CreateErr24, description: "TX Errores u observadas 24h");

        _meter.CreateObservableGauge("tx_resolved_count_24h", CreateRes24, description: "TX Resueltas o completadas 24h");
        _meter.CreateObservableGauge("tx_resolution_avg_seconds", CreateResAvg, unit: "s", description: "Promedio de duración en segundos hasta resolución 24h");

        // =========================
        // Tabular Operational Metrics
        // =========================
        _meter.CreateObservableGauge("tx_state_count_24h", () => 
            MapStates(_state.Current?.IntraStateCount24h, _state.Current?.InterStateCount24h), description: "Distribución por estado");
            
        _meter.CreateObservableGauge("tx_pending_current_count", () => 
            MapStates(_state.Current?.IntraPendingCurrent, _state.Current?.InterPendingCurrent), description: "Backlog actual por estado");
            
        _meter.CreateObservableGauge("tx_pending_aging_bucket_count", CreateAgingBuckets, description: "Aging limits por bucket");
        
        _meter.CreateObservableGauge("tx_pending_avg_age_seconds", () => 
            MapAgeStat(_state.Current?.IntraAgeStats, _state.Current?.InterAgeStats, true), description: "Promedio edad por estado");
        _meter.CreateObservableGauge("tx_pending_max_age_seconds", () => 
            MapAgeStat(_state.Current?.IntraAgeStats, _state.Current?.InterAgeStats, false), description: "Max edad por estado");

        _meter.CreateObservableGauge("tx_rejected_count_24h", () => 
            MapFailures(_state.Current?.IntraFailures, _state.Current?.InterFailures, true), description: "Rechazos totales");
        _meter.CreateObservableGauge("tx_failed_technical_count_24h", () => 
            MapFailures(_state.Current?.IntraFailures, _state.Current?.InterFailures, false), description: "Fallas tecnicas totales");

        _meter.CreateObservableGauge("tx_type_count_24h", () => 
            MapTypeCount(_state.Current?.IntraTypeCount, _state.Current?.InterTypeCount), description: "Volumen 24h agrupado por tipo");

        _meter.CreateObservableGauge("tx_amount_total_24h", () => 
            MapAmountTotal(_state.Current?.IntraAmountTotal, _state.Current?.InterAmountTotal), description: "Total transado por moneda");

        _meter.CreateObservableGauge("tx_amount_by_type_24h", () => 
            MapAmountByType(_state.Current?.IntraAmountByType, _state.Current?.InterAmountByType), description: "Importes transados por tipo y moneda");

        _meter.CreateObservableGauge("tx_success_avg_seconds", () => 
            MapSpeedStat(_state.Current?.IntraSuccessSpeed, _state.Current?.InterSuccessSpeed, true), description: "AVG speed proxy");
        _meter.CreateObservableGauge("tx_success_p95_seconds", () => 
            MapSpeedStat(_state.Current?.IntraSuccessSpeed, _state.Current?.InterSuccessSpeed, false), description: "P95 speed proxy");

        // Destination Bank specifics
        _meter.CreateObservableGauge("tx_interbank_bank_count_24h", CreateBankCount, description: "Volumen 24h por banco");
        _meter.CreateObservableGauge("tx_interbank_bank_state_count_24h", CreateBankStateCount, description: "Estado 24h por banco");
        _meter.CreateObservableGauge("tx_interbank_bank_amount_total_24h", CreateBankAmount, description: "Importes 24h por banco");
    }

    // --- Legacy / Base Creators ---
    private IEnumerable<Measurement<int>> CreateVolObs() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraTxCreated15m, new KeyValuePair<string, object?>("source", "intra")),
        new Measurement<int>(s.InterTxCreated15m, new KeyValuePair<string, object?>("source", "inter"))});

    private IEnumerable<Measurement<int>> CreateVolObs24() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraTxCreated24h, new KeyValuePair<string, object?>("source", "intra")),
        new Measurement<int>(s.InterTxCreated24h, new KeyValuePair<string, object?>("source", "inter"))});

    private IEnumerable<Measurement<int>> CreateVolObs7() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraTxCreated7d, new KeyValuePair<string, object?>("source", "intra")),
        new Measurement<int>(s.InterTxCreated7d, new KeyValuePair<string, object?>("source", "inter"))});

    private IEnumerable<Measurement<int>> CreateVolObs30() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraTxCreated30d, new KeyValuePair<string, object?>("source", "intra")),
        new Measurement<int>(s.InterTxCreated30d, new KeyValuePair<string, object?>("source", "inter"))});

    private IEnumerable<Measurement<int>> CreatePend24() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraPendingCount24h, new KeyValuePair<string, object?>("source", "intra")),
        new Measurement<int>(s.InterPendingCount24h, new KeyValuePair<string, object?>("source", "inter"))});

    private IEnumerable<Measurement<int>> CreatePend7() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraPendingCount7d, new KeyValuePair<string, object?>("source", "intra")),
        new Measurement<int>(s.InterPendingCount7d, new KeyValuePair<string, object?>("source", "inter"))});

    private IEnumerable<Measurement<int>> CreatePendOldest() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraPendingOldestSec, new KeyValuePair<string, object?>("source", "intra")),
        new Measurement<int>(s.InterPendingOldestSec, new KeyValuePair<string, object?>("source", "inter"))});

    private IEnumerable<Measurement<int>> CreateErr24() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraErrorCount24h, new KeyValuePair<string, object?>("source", "intra")),
        new Measurement<int>(s.InterErrorCount24h, new KeyValuePair<string, object?>("source", "inter"))});

    private IEnumerable<Measurement<int>> CreateRes24() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraResolvedCount24h, new KeyValuePair<string, object?>("source", "intra")),
        new Measurement<int>(s.InterResolvedCount24h, new KeyValuePair<string, object?>("source", "inter"))});

    private IEnumerable<Measurement<int>> CreateResAvg() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraResAvgSec, new KeyValuePair<string, object?>("source", "intra")),
        new Measurement<int>(s.InterResAvgSec, new KeyValuePair<string, object?>("source", "inter"))});

    // --- Tabular Mapping ---
    
    private IEnumerable<Measurement<int>> MapStates(IReadOnlyList<SqlPollingClient.StateCountRow> intra, IReadOnlyList<SqlPollingClient.StateCountRow> inter)
    {
        if (intra != null) foreach(var x in intra) yield return new Measurement<int>(x.Count, Kvp("source", "intra"), Kvp("estado", x.Estado.ToString()));
        if (inter != null) foreach(var x in inter) yield return new Measurement<int>(x.Count, Kvp("source", "inter"), Kvp("estado", x.Estado.ToString()));
    }

    private IEnumerable<Measurement<int>> CreateAgingBuckets()
    {
        var s = _state.Current;
        if (s == null) yield break;

        if (s.IntraPendingBucket != null) foreach(var x in s.IntraPendingBucket)
        {
            if (x.Ge14400s > 0) yield return new(x.Ge14400s, Kvp("source", "intra"), Kvp("estado", x.Estado.ToString()), Kvp("bucket", "ge_14400s"));
            if (x.Ge3600s > 0) yield return new(x.Ge3600s, Kvp("source", "intra"), Kvp("estado", x.Estado.ToString()), Kvp("bucket", "ge_3600s"));
            if (x.Ge900s > 0) yield return new(x.Ge900s, Kvp("source", "intra"), Kvp("estado", x.Estado.ToString()), Kvp("bucket", "ge_900s"));
        }
        if (s.InterPendingBucket != null) foreach(var x in s.InterPendingBucket)
        {
            if (x.Ge14400s > 0) yield return new(x.Ge14400s, Kvp("source", "inter"), Kvp("estado", x.Estado.ToString()), Kvp("bucket", "ge_14400s"));
            if (x.Ge3600s > 0) yield return new(x.Ge3600s, Kvp("source", "inter"), Kvp("estado", x.Estado.ToString()), Kvp("bucket", "ge_3600s"));
            if (x.Ge900s > 0) yield return new(x.Ge900s, Kvp("source", "inter"), Kvp("estado", x.Estado.ToString()), Kvp("bucket", "ge_900s"));
        }
    }

    private IEnumerable<Measurement<int>> MapAgeStat(IReadOnlyList<SqlPollingClient.AgeStatsRow> intra, IReadOnlyList<SqlPollingClient.AgeStatsRow> inter, bool avg)
    {
        if (intra != null) foreach(var x in intra) yield return new Measurement<int>(avg ? x.AvgSec : x.MaxSec, Kvp("source", "intra"), Kvp("estado", x.Estado.ToString()));
        if (inter != null) foreach(var x in inter) yield return new Measurement<int>(avg ? x.AvgSec : x.MaxSec, Kvp("source", "inter"), Kvp("estado", x.Estado.ToString()));
    }

    private IEnumerable<Measurement<int>> MapFailures(SqlPollingClient.FailuresRow intra, SqlPollingClient.FailuresRow inter, bool rejected)
    {
        if (intra != null) yield return new Measurement<int>(rejected ? intra.Rejected : intra.FailedTechnical, Kvp("source", "intra"));
        if (inter != null) yield return new Measurement<int>(rejected ? inter.Rejected : inter.FailedTechnical, Kvp("source", "inter"));
    }

    private IEnumerable<Measurement<int>> MapTypeCount(IReadOnlyList<SqlPollingClient.TypeCountRow> intra, IReadOnlyList<SqlPollingClient.TypeCountRow> inter)
    {
        if (intra != null) foreach(var x in intra) yield return new(x.Count, Kvp("source", "intra"), Kvp("tipo", x.Tipo.ToString()));
        if (inter != null) foreach(var x in inter) yield return new(x.Count, Kvp("source", "inter"), Kvp("tipo", x.Tipo.ToString()));
    }

    private IEnumerable<Measurement<double>> MapAmountTotal(IReadOnlyList<SqlPollingClient.AmountTotalRow> intra, IReadOnlyList<SqlPollingClient.AmountTotalRow> inter)
    {
        if (intra != null) foreach(var x in intra) yield return new(x.Total, Kvp("source", "intra"), Kvp("moneda", x.Moneda.ToString()));
        if (inter != null) foreach(var x in inter) yield return new(x.Total, Kvp("source", "inter"), Kvp("moneda", x.Moneda.ToString()));
    }

    private IEnumerable<Measurement<double>> MapAmountByType(IReadOnlyList<SqlPollingClient.AmountByTypeRow> intra, IReadOnlyList<SqlPollingClient.AmountByTypeRow> inter)
    {
        if (intra != null) foreach(var x in intra) yield return new(x.Total, Kvp("source", "intra"), Kvp("tipo", x.Tipo.ToString()), Kvp("moneda", x.Moneda.ToString()));
        if (inter != null) foreach(var x in inter) yield return new(x.Total, Kvp("source", "inter"), Kvp("tipo", x.Tipo.ToString()), Kvp("moneda", x.Moneda.ToString()));
    }

    private IEnumerable<Measurement<int>> MapSpeedStat(IReadOnlyList<SqlPollingClient.SpeedStatsRow> intra, IReadOnlyList<SqlPollingClient.SpeedStatsRow> inter, bool avg)
    {
        if (intra != null) foreach(var x in intra) yield return new Measurement<int>(avg ? x.AvgSec : x.P95Sec, Kvp("source", "intra"), Kvp("tipo", x.Tipo.ToString()));
        if (inter != null) foreach(var x in inter) yield return new Measurement<int>(avg ? x.AvgSec : x.P95Sec, Kvp("source", "inter"), Kvp("tipo", x.Tipo.ToString()));
    }

    private IEnumerable<Measurement<int>> CreateBankCount()
    {
        var src = _state.Current?.InterBankCount;
        if (src != null) foreach(var x in src) yield return new(x.Count, Kvp("banco", x.Banco.ToString()));
    }

    private IEnumerable<Measurement<int>> CreateBankStateCount()
    {
        var src = _state.Current?.InterBankStateCount;
        if (src != null) foreach(var x in src) yield return new(x.Count, Kvp("banco", x.Banco.ToString()), Kvp("estado", x.Estado.ToString()));
    }

    private IEnumerable<Measurement<double>> CreateBankAmount()
    {
        var src = _state.Current?.InterBankAmountTotal;
        if (src != null) foreach(var x in src) yield return new(x.Total, Kvp("banco", x.Banco.ToString()), Kvp("moneda", x.Moneda.ToString()));
    }


    // Helpers
    private IEnumerable<Measurement<int>> SafeGen(System.Func<SqlPollingClient.Snapshot, IEnumerable<Measurement<int>>> generator)
    {
        var snapshot = _state.Current;
        return snapshot == null ? Enumerable.Empty<Measurement<int>>() : generator(snapshot);
    }
    
    private KeyValuePair<string, object?> Kvp(string k, string v) => new KeyValuePair<string, object?>(k, v);

    public void RecordPollSuccess(double durationSeconds)
    {
        long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Interlocked.Exchange(ref _lastPollUnixTime, now);
        Interlocked.Exchange(ref _consecutiveFailures, 0);
        _pollDurationSeconds.Record(durationSeconds);
    }

    public void RecordPollError(double durationSeconds)
    {
        _pollErrorTotal.Add(1);
        Interlocked.Increment(ref _consecutiveFailures);
        _pollDurationSeconds.Record(durationSeconds);
    }

    public void Dispose() => _meter.Dispose();
}
