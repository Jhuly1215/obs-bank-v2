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
            MapStates(_state.Current?.IntraOpPending, _state.Current?.InterOpPending), description: "Backlog actual (Para compatibilidad dashboard)");
            
        _meter.CreateObservableGauge("tx_pending_aging_bucket_count", CreateAgingBuckets, description: "Aging limits por bucket");
        
        // Note: Avg/Max Age stats are now handled via ReviewStats for the critical path

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

        // =========================
        // New Taxonomy & Critical Metrics
        // =========================
        _meter.CreateObservableGauge("tx_op_pending_count", () => 
            MapStates(_state.Current?.IntraOpPending, _state.Current?.InterOpPending), description: "Backlog operativo real (1,6)");
            
        _meter.CreateObservableGauge("tx_programmed_count", () => 
            MapStates(_state.Current?.IntraProgrammed, _state.Current?.InterProgrammed), description: "Backlog programado (0,7,17)");
        _meter.CreateObservableGauge("tx_programmed_current_count", () => 
            MapStates(_state.Current?.IntraProgrammed, _state.Current?.InterProgrammed), description: "Alias compatibilidad backlog programado actual");

        _meter.CreateObservableGauge("tx_review_count", CreateReviewCount, description: "Conteo en estado 100");
        _meter.CreateObservableGauge("tx_review_current_count", CreateReviewCount, description: "Alias compatibilidad review actual");
        _meter.CreateObservableGauge("tx_review_avg_age_seconds", CreateReviewAvg, unit: "s", description: "Promedio edad estado 100");
        _meter.CreateObservableGauge("tx_review_max_age_seconds", CreateReviewMax, unit: "s", description: "Max edad estado 100");
        _meter.CreateObservableGauge("tx_review_dead_count", CreateReviewDead, description: "Estado 100 estancado > 300s");

        _meter.CreateObservableGauge("tx_pending_avg_age_seconds", () => 
            MapAgeStat(_state.Current?.IntraPendingAgeStats, _state.Current?.InterPendingAgeStats, true), 
            unit: "s", description: "Edad promedio del backlog por estado");
        _meter.CreateObservableGauge("tx_pending_max_age_seconds", () => 
            MapAgeStat(_state.Current?.IntraPendingAgeStats, _state.Current?.InterPendingAgeStats, false), 
            unit: "s", description: "Edad maxima del backlog por estado");

        _meter.CreateObservableGauge("tx_success_count_24h", CreateSuccess24, description: "TX exitosas 24h");
        
        _meter.CreateObservableGauge("tx_compensated_count_24h", CreateCompensatedCount, description: "TX Compensadas/Despignoradas (9)");
        _meter.CreateObservableGauge("tx_compensated_current_count", CreateCompensatedCurrentCount, description: "Inventario actual compensadas");
        _meter.CreateObservableGauge("tx_closed_count_24h", CreateClosedCount24, description: "TX Cerradas/Concluidas en 24h");
        _meter.CreateObservableGauge("tx_other_state_count_24h", CreateOtherStateCount24, description: "TX en estados intermedios/otros 24h");

        // =========================
        // High Resolution & Anomalies (Optimized)
        // =========================
        _meter.CreateObservableGauge("tx_created_total_5m", CreateVolObs5m, description: "TX Creadas 5m");
        _meter.CreateObservableGauge("tx_created_total_1h", CreateVolObs1h, description: "TX Creadas 1h");
        _meter.CreateObservableGauge("tx_amount_total_1h", CreateAmount1h, unit: "$", description: "Total transado 1h");
        
        _meter.CreateObservableGauge("tx_success_p99_seconds", CreateSpeedP99, unit: "s", description: "P99 speed proxy 24h");
        _meter.CreateObservableGauge("tx_anomaly_zero_duration_count_24h", CreateZeroDur, description: "TX resueltas en 0s (posible inconsistencia)");
        _meter.CreateObservableGauge("tx_anomaly_missing_mod_count_24h", CreateMissingMod, description: "TX resueltas sin fecha de modificacion");

        // Destination Bank specifics
        _meter.CreateObservableGauge("tx_interbank_bank_count_24h", CreateBankCount, description: "Volumen 24h por banco");
        _meter.CreateObservableGauge("tx_interbank_bank_state_count_24h", CreateBankStateCount, description: "Estado 24h por banco");
        _meter.CreateObservableGauge("tx_interbank_bank_amount_total_24h", CreateBankAmount, description: "Importes 24h por banco");

        // =========================
        // Health & Infrastructure
        // =========================
        _meter.CreateObservableGauge("sql_server_sessions_count", CreateSessionMetrics, description: "Sesiones activas agrupadas por estado y espera");
        _meter.CreateObservableGauge("sql_server_io_stall_seconds", CreateIoMetrics, unit: "s", description: "Latencia acumulada de E/S por archivo (I/O Stall)");
        _meter.CreateObservableGauge("sql_server_io_bytes_total", CreateIoBytesMetrics, unit: "By", description: "Bytes leidos/escritos acumulados por archivo");
        _meter.CreateObservableGauge("sql_database_size_mb", CreateDatabaseSizeMetrics, unit: "MB", description: "Tamaño y uso de archivos de base de datos");
        
        // =========================
        // Data Quality Anomalies (Granular)
        // =========================
        _meter.CreateObservableGauge("tx_quality_anomaly_count", CreateAnomalyMetrics, description: "Anomalías de calidad de datos detectadas");
        _meter.CreateObservableGauge("tx_quality_missing_mod_count_24h", CreateQualityMissingMod, description: "TX exitosas sin fechaModificacion");
        _meter.CreateObservableGauge("tx_quality_zero_duration_count_24h", CreateQualityZeroDur, description: "TX exitosas con duracion 0");
        _meter.CreateObservableGauge("tx_quality_negative_duration_count_24h", CreateQualityNegDur, description: "TX exitosas con duracion negativa");
    }

    // --- Legacy / Base Creators ---
    private IEnumerable<Measurement<int>> CreateVolObs() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraTxCreated15m, Tags(s, "intra")),
        new Measurement<int>(s.InterTxCreated15m, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreateVolObs24() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraTxCreated24h, Tags(s, "intra")),
        new Measurement<int>(s.InterTxCreated24h, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreateVolObs7() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraTxCreated7d, Tags(s, "intra")),
        new Measurement<int>(s.InterTxCreated7d, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreateVolObs30() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraTxCreated30d, Tags(s, "intra")),
        new Measurement<int>(s.InterTxCreated30d, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreatePend24() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraPendingCount24h, Tags(s, "intra")),
        new Measurement<int>(s.InterPendingCount24h, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreatePend7() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraPendingCount7d, Tags(s, "intra")),
        new Measurement<int>(s.InterPendingCount7d, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreatePendOldest() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraPendingOldestSec, Tags(s, "intra")),
        new Measurement<int>(s.InterPendingOldestSec, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreateErr24() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraErrorCount24h, Tags(s, "intra")),
        new Measurement<int>(s.InterErrorCount24h, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreateRes24() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraResolvedCount24h, Tags(s, "intra")),
        new Measurement<int>(s.InterResolvedCount24h, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreateResAvg() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraResAvgSec, Tags(s, "intra")),
        new Measurement<int>(s.InterResAvgSec, Tags(s, "inter"))});

    // --- New High-Res & Anomaly Creators ---
    private IEnumerable<Measurement<int>> CreateVolObs5m() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraTxCreated5m, Tags(s, "intra")),
        new Measurement<int>(s.InterTxCreated5m, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreateVolObs1h() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraTxCreated1h, Tags(s, "intra")),
        new Measurement<int>(s.InterTxCreated1h, Tags(s, "inter"))});

    private IEnumerable<Measurement<double>> CreateAmount1h() => SnapshotSafeGen(s => new[] {
        new Measurement<double>(s.IntraAmountTotal1h, Tags(s, "intra")),
        new Measurement<double>(s.InterAmountTotal1h, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreateSpeedP99() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraSuccessSpeedP99, Tags(s, "intra")),
        new Measurement<int>(s.InterSuccessSpeedP99, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreateReviewCount() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraReview?.TotalCount ?? 0, Tags(s, "intra")),
        new Measurement<int>(s.InterReview?.TotalCount ?? 0, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreateReviewAvg() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraReview?.AvgSec ?? 0, Tags(s, "intra")),
        new Measurement<int>(s.InterReview?.AvgSec ?? 0, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreateReviewMax() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraReview?.MaxSec ?? 0, Tags(s, "intra")),
        new Measurement<int>(s.InterReview?.MaxSec ?? 0, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreateReviewDead() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraReview?.DeadCount ?? 0, Tags(s, "intra")),
        new Measurement<int>(s.InterReview?.DeadCount ?? 0, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreateCompensatedCount() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraFailures?.Compensated ?? 0, Tags(s, "intra")),
        new Measurement<int>(s.InterFailures?.Compensated ?? 0, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreateZeroDur() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraZeroDurationCount, Tags(s, "intra")),
        new Measurement<int>(s.InterZeroDurationCount, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreateMissingMod() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraMissingModCount, Tags(s, "intra")),
        new Measurement<int>(s.InterMissingModCount, Tags(s, "inter"))});

    // --- Tabular Mapping ---
    
    private IEnumerable<Measurement<int>> MapStates(IReadOnlyList<SqlPollingClient.StateCountRow> intra, IReadOnlyList<SqlPollingClient.StateCountRow> inter)
    {
        var s = _state.Current;
        if (intra != null) foreach(var x in intra) yield return new Measurement<int>(x.Count, Tags(s, "intra", "estado", x.Estado.ToString()));
        if (inter != null) foreach(var x in inter) yield return new Measurement<int>(x.Count, Tags(s, "inter", "estado", x.Estado.ToString()));
    }

    private IEnumerable<Measurement<int>> CreateAgingBuckets()
    {
        var s = _state.Current;
        if (s == null) yield break;

        if (s.IntraPendingBucket != null) foreach(var x in s.IntraPendingBucket)
        {
            if (x.Ge14400s > 0) yield return new(x.Ge14400s, Tags(s, "intra", "estado", x.Estado.ToString(), "bucket", "ge_14400s"));
            if (x.Ge3600s > 0) yield return new(x.Ge3600s, Tags(s, "intra", "estado", x.Estado.ToString(), "bucket", "ge_3600s"));
            if (x.Ge900s > 0) yield return new(x.Ge900s, Tags(s, "intra", "estado", x.Estado.ToString(), "bucket", "ge_900s"));
        }
        if (s.InterPendingBucket != null) foreach(var x in s.InterPendingBucket)
        {
            if (x.Ge14400s > 0) yield return new(x.Ge14400s, Tags(s, "inter", "estado", x.Estado.ToString(), "bucket", "ge_14400s"));
            if (x.Ge3600s > 0) yield return new(x.Ge3600s, Tags(s, "inter", "estado", x.Estado.ToString(), "bucket", "ge_3600s"));
            if (x.Ge900s > 0) yield return new(x.Ge900s, Tags(s, "inter", "estado", x.Estado.ToString(), "bucket", "ge_900s"));
        }
    }

    private IEnumerable<Measurement<int>> MapAgeStat(IReadOnlyList<SqlPollingClient.AgeStatsRow> intra, IReadOnlyList<SqlPollingClient.AgeStatsRow> inter, bool avg)
    {
        var s = _state.Current;
        if (intra != null) foreach(var x in intra) yield return new Measurement<int>(avg ? x.AvgSec : x.MaxSec, Tags(s, "intra", "estado", x.Estado.ToString()));
        if (inter != null) foreach(var x in inter) yield return new Measurement<int>(avg ? x.AvgSec : x.MaxSec, Tags(s, "inter", "estado", x.Estado.ToString()));
    }

    private IEnumerable<Measurement<int>> MapFailures(SqlPollingClient.FailuresRow intra, SqlPollingClient.FailuresRow inter, bool rejected)
    {
        var s = _state.Current;
        if (intra != null) yield return new Measurement<int>(rejected ? intra.Rejected : intra.FailedTechnical, Tags(s, "intra"));
        if (inter != null) yield return new Measurement<int>(rejected ? inter.Rejected : inter.FailedTechnical, Tags(s, "inter"));
    }

    private IEnumerable<Measurement<int>> MapTypeCount(IReadOnlyList<SqlPollingClient.TypeCountRow> intra, IReadOnlyList<SqlPollingClient.TypeCountRow> inter)
    {
        var s = _state.Current;
        if (intra != null) foreach(var x in intra) yield return new(x.Count, Tags(s, "intra", "tipo", x.Tipo.ToString()));
        if (inter != null) foreach(var x in inter) yield return new(x.Count, Tags(s, "inter", "tipo", x.Tipo.ToString()));
    }

    private IEnumerable<Measurement<double>> MapAmountTotal(IReadOnlyList<SqlPollingClient.AmountTotalRow> intra, IReadOnlyList<SqlPollingClient.AmountTotalRow> inter)
    {
        var s = _state.Current;
        if (intra != null) foreach(var x in intra) yield return new(x.Total, Tags(s, "intra", "moneda", x.Moneda.ToString()));
        if (inter != null) foreach(var x in inter) yield return new(x.Total, Tags(s, "inter", "moneda", x.Moneda.ToString()));
    }

    private IEnumerable<Measurement<double>> MapAmountByType(IReadOnlyList<SqlPollingClient.AmountByTypeRow> intra, IReadOnlyList<SqlPollingClient.AmountByTypeRow> inter)
    {
        var s = _state.Current;
        if (intra != null) foreach(var x in intra) yield return new(x.Total, Tags(s, "intra", "tipo", x.Tipo.ToString(), "moneda", x.Moneda.ToString()));
        if (inter != null) foreach(var x in inter) yield return new(x.Total, Tags(s, "inter", "tipo", x.Tipo.ToString(), "moneda", x.Moneda.ToString()));
    }

    private IEnumerable<Measurement<int>> MapSpeedStat(IReadOnlyList<SqlPollingClient.SpeedStatsRow> intra, IReadOnlyList<SqlPollingClient.SpeedStatsRow> inter, bool avg)
    {
        var s = _state.Current;
        if (intra != null) foreach(var x in intra) yield return new Measurement<int>(avg ? x.AvgSec : x.P95Sec, Tags(s, "intra", "tipo", x.Tipo.ToString()));
        if (inter != null) foreach(var x in inter) yield return new Measurement<int>(avg ? x.AvgSec : x.P95Sec, Tags(s, "inter", "tipo", x.Tipo.ToString()));
    }

    private IEnumerable<Measurement<int>> CreateBankCount()
    {
        var s = _state.Current;
        if (s?.InterBankCount != null) foreach(var x in s.InterBankCount) yield return new(x.Count, Tags(s, "inter", "banco", x.Banco.ToString()));
    }

    private IEnumerable<Measurement<int>> CreateBankStateCount()
    {
        var s = _state.Current;
        if (s?.InterBankStateCount != null) foreach(var x in s.InterBankStateCount) yield return new(x.Count, Tags(s, "inter", "banco", x.Banco.ToString(), "estado", x.Estado.ToString()));
    }

    private IEnumerable<Measurement<double>> CreateBankAmount()
    {
        var s = _state.Current;
        if (s?.InterBankAmountTotal != null) foreach(var x in s.InterBankAmountTotal) yield return new(x.Total, Tags(s, "inter", "banco", x.Banco.ToString(), "moneda", x.Moneda.ToString()));
    }

    // --- Health & Quality Creators ---

    private IEnumerable<Measurement<int>> CreateSessionMetrics() => SafeGen(s => 
        s.ServerSessions?.Select(x => new Measurement<int>(x.Count, Tags(s, null, "status", x.Status, "wait_type", x.WaitType))) 
        ?? Enumerable.Empty<Measurement<int>>());

    private IEnumerable<Measurement<double>> CreateIoMetrics() => SnapshotSafeGen(s => 
        s.FileIoStats?.SelectMany(x => new[] {
            new Measurement<double>(x.ReadStallMs / 1000.0, Tags(s, null, "file_id", x.FileId.ToString(), "io_type", "read")),
            new Measurement<double>(x.WriteStallMs / 1000.0, Tags(s, null, "file_id", x.FileId.ToString(), "io_type", "write"))
        }) ?? Enumerable.Empty<Measurement<double>>());

    private IEnumerable<Measurement<long>> CreateIoBytesMetrics()
    {
        var s = _state.Current;
        if (s?.FileIoStats == null) return Enumerable.Empty<Measurement<long>>();
        return s.FileIoStats.SelectMany(x => new[] {
            new Measurement<long>(x.BytesRead, Tags(s, null, "file_id", x.FileId.ToString(), "io_type", "read")),
            new Measurement<long>(x.BytesWritten, Tags(s, null, "file_id", x.FileId.ToString(), "io_type", "write"))
        });
    }

    private IEnumerable<Measurement<int>> CreateDatabaseSizeMetrics() => SafeGen(s => 
        s.DatabaseSizes?.SelectMany(x => new[] {
            new Measurement<int>(x.SizeMB, Tags(s, null, "file_name", x.FileName, "stat", "total")),
            new Measurement<int>(x.UsedMB, Tags(s, null, "file_name", x.FileName, "stat", "used"))
        }) ?? Enumerable.Empty<Measurement<int>>());

    private IEnumerable<Measurement<int>> CreateAnomalyMetrics() => SafeGen(s => 
        s.QualityAnomalies?.SelectMany(x => new[] {
            new Measurement<int>(x.MissingMod, Tags(s, x.Source, "tipo", x.Tipo.ToString(), "estado", x.Estado.ToString(), "anomaly", "missing_mod")),
            new Measurement<int>(x.ZeroDur, Tags(s, x.Source, "tipo", x.Tipo.ToString(), "estado", x.Estado.ToString(), "anomaly", "zero_duration")),
            new Measurement<int>(x.NegDur, Tags(s, x.Source, "tipo", x.Tipo.ToString(), "estado", x.Estado.ToString(), "anomaly", "negative_duration"))
        }) ?? Enumerable.Empty<Measurement<int>>());

    private IEnumerable<Measurement<int>> CreateSuccess24() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraSuccessCount24h, Tags(s, "intra")),
        new Measurement<int>(s.InterSuccessCount24h, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreateQualityMissingMod() => SafeGen(s =>
        s.QualityAnomalies?.Select(x => new Measurement<int>(x.MissingMod, Tags(s, x.Source, "tipo", x.Tipo.ToString(), "estado", x.Estado.ToString())))
        ?? Enumerable.Empty<Measurement<int>>());

    private IEnumerable<Measurement<int>> CreateQualityZeroDur() => SafeGen(s =>
        s.QualityAnomalies?.Select(x => new Measurement<int>(x.ZeroDur, Tags(s, x.Source, "tipo", x.Tipo.ToString(), "estado", x.Estado.ToString())))
        ?? Enumerable.Empty<Measurement<int>>());

    private IEnumerable<Measurement<int>> CreateQualityNegDur() => SafeGen(s =>
        s.QualityAnomalies?.Select(x => new Measurement<int>(x.NegDur, Tags(s, x.Source, "tipo", x.Tipo.ToString(), "estado", x.Estado.ToString())))
        ?? Enumerable.Empty<Measurement<int>>());

    private IEnumerable<Measurement<int>> CreateClosedCount24() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraClosedCount24h, Tags(s, "intra")),
        new Measurement<int>(s.InterClosedCount24h, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreateOtherStateCount24() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraOtherStateCount24h, Tags(s, "intra")),
        new Measurement<int>(s.InterOtherStateCount24h, Tags(s, "inter"))});

    private IEnumerable<Measurement<int>> CreateCompensatedCurrentCount() => SafeGen(s => new[] {
        new Measurement<int>(s.IntraCompensatedCurrentCount, Tags(s, "intra")),
        new Measurement<int>(s.InterCompensatedCurrentCount, Tags(s, "inter"))});

    // Helpers
    private IEnumerable<Measurement<int>> SafeGen(System.Func<SqlPollingClient.Snapshot, IEnumerable<Measurement<int>>> generator)
    {
        var snapshot = _state.Current;
        return snapshot == null ? Enumerable.Empty<Measurement<int>>() : generator(snapshot);
    }

    private IEnumerable<Measurement<double>> SnapshotSafeGen(System.Func<SqlPollingClient.Snapshot, IEnumerable<Measurement<double>>> generator)
    {
        var snapshot = _state.Current;
        return snapshot == null ? Enumerable.Empty<Measurement<double>>() : generator(snapshot);
    }
    
    private KeyValuePair<string, object?>[] Tags(SqlPollingClient.Snapshot? s, string? source = null, string? k1 = null, string? v1 = null, string? k2 = null, string? v2 = null, string? k3 = null, string? v3 = null)
    {
        var list = new List<KeyValuePair<string, object?>>();
        if (s != null) list.Add(new("tipo_dia", s.DayType));
        if (source != null) list.Add(new("source", source));
        if (k1 != null) list.Add(new(k1, v1));
        if (k2 != null) list.Add(new(k2, v2));
        if (k3 != null) list.Add(new(k3, v3));
        return list.ToArray();
    }

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
