# Econet Transactional Monitoring - Refined Taxonomy & Health

This document outlines the refactored transactional monitoring logic for Econet, implemented in the `Bank.Obs.SqlPoller`.

## 1. Dynamic Context: Day Type Tagging

To allow dynamic thresholds in Grafana without changing SQL code, every metric exported by the poller is tagged with `tipo_dia`.

| Value | Days | Use Case |
| :--- | :--- | :--- |
| **habil** | Mon - Fri | High-volume operational baselines and strict alerts. |
| **fin_de_semana** | Sat - Sun | Lower-volume maintenance baselines and relaxed alerts. |

---

## 2. Refined Backlog Taxonomy

The backlog is no longer a single number. It is separated into functional groups to allow precise alerting:

| Gauge | States | Description | Alerting Group |
| :--- | :--- | :--- | :--- |
| `tx_op_pending_count` | 1, 6 | Actively being processed. | Operations |
| `tx_programmed_count` | 0, 7, 17 | Scheduled for the future. | Visibility Only |
| `tx_review_count` | 100 | Stalled/Waiting for review. | Support/Middleware |
| `tx_review_dead_count` | 100 (>300s) | **Critical At-Risk**. Stalled for more than 5 minutes. | Critical/Incident |

---

## 3. Infrastructure & Server Health (Corrected)

We have separated Resource monitoring into distinct, low-impact queries:

### A. Active Sessions & Waits
Tracks what SQL Server is doing *right now*.
- `sql_server_sessions_count`: Grouped by `status` (running, runnable, sleeping) and `wait_type`. Use to detect contention.

### B. Real File I/O
Tracks cumulative latency and throughput at the OS file level.
- `sql_server_io_stall_seconds`: Cumulative delay in E/S. Use `rate()` in Grafana to see real-time latency.
- `sql_server_io_bytes_total`: Throughput (Read/Write) per file.

### C. Database Storage
- `sql_database_size_mb`: Overall size and used space per file (Data vs Log).

---

## 4. Data Quality Anomalies

The following metrics detect inconsistencies in the transactional engine (Filtered by last 24h):
- `tx_quality_anomaly_count`: Grouped by `anomaly` type:
    - `missing_mod`: Success TX without a modification date.
    - `zero_duration`: End-to-end processing of 0 seconds.
    - `negative_duration`: Inconsistent timestamps (`fechaModificacion < fechaOperacion`).

---

## 5. Statistical Baseline Strategy (120 Days)

Running 120-day averages every 60s is **not feasible** for polling.
**Recommendation**:
1.  Implement a Daily SQL Job that calculates the `p50/p95/p99` per hour and day type for the last 120 days.
2.  Store results in a `Monitoring_Baselines` table.
3.  The Poller reads the current hour's baseline once per cycle and exports it as `tx_baseline_volume_total`.
4.  Grafana compares `tx_created_total_1h` vs `tx_baseline_volume_total`.

---

## 6. SQL Indexing Strategy (Proposal)

To keep the Poller under 100ms, apply these indexes:
- `IX_Transferencia_Estado_FechaOp`: (estado, fechaOperacion) INCLUDE (fechaModificacion, monto)
- `IX_Inter_Estado_FechaOp`: (estado, fechaOperacion) INCLUDE (fechaModificacion, monto, bancoDestino)
