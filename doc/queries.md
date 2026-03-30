# Consultas y Métricas de Observabilidad (sql-poller)

El servicio `Bank.Obs.SqlPoller` ejecuta un ciclo (*polling*) continuo cada minuto sobre la base de datos de Transacciones de Ecofuturo para recolectar indicadores operativos críticos (Volumen, Pendientes y Errores Técnicos).

Toda la instrumentación se realiza nativamente con la librería de **OpenTelemetry en .NET 9**. Las métricas son capturadas por el colector y expuestas a Prometheus.

Debido a que OpenTelemetry agrega la **unidad de medida** (`tx`, `ratio`, `min`) al final del nombre de la métrica de forma automática al exportarla hacia Prometheus, la siguiente tabla mapea con precisión la variable de estado C#, la métrica definida y el resultado final a consultar en Grafana.

---

## 1. Volumen Transaccional (Conteos Activos)

Se mide la cantidad neta de operaciones que llegaron al sistema (`Transferencia` para internas, y `TransferenciaInterbancaria` para externas).

| Consulta SQL / C# | Ventana | Descripción | Nombre OTel (.NET) | Nombre Final PromQL (Grafana) |
|-------------------|---------|-------------|-------------------|-------------------------------|
| `IntraTxLast15m` | Últimos 15 min | Operaciones internas recibidas. | `bank_intra_tx_15m_total` | `bank_intra_tx_15m_total_tx` |
| `InterTxLast15m` | Últimos 15 min | Operaciones interbancarias recibidas. | `bank_inter_tx_15m_total` | `bank_inter_tx_15m_total_tx` |
| `IntraTxLast24h` | Últimas 24h | Total bruto transferencias internas (1 día). | `bank_intra_tx_24h_total` | `bank_intra_tx_24h_total_tx` |
| `InterTxLast24h` | Últimas 24h | Total bruto transferencias interbancarias (1 día).| `bank_inter_tx_24h_total` | `bank_inter_tx_24h_total_tx` |
| `IntraTxLast30d` | Últimos 30d | Rendimiento de volumen mensual (interno). | `bank_intra_tx_30d_total` | `bank_intra_tx_30d_total_tx` |
| `InterTxLast30d` | Últimos 30d | Rendimiento de volumen mensual (interbancario). | `bank_inter_tx_30d_total` | `bank_inter_tx_30d_total_tx` |

---

## 2. Backlog y Transacciones Pendientes (Atascadas)

Monitoriza operaciones atrapadas en estados intermedios del core bancario (estados 1 al 8 de la máquina de estados local).
**NOTA:** El Estado 9 significa "Finalizado con Error / Rechazado", no es un pendiente.

| Consulta SQL / C# | Ventana | Descripción | Nombre Final PromQL (Grafana) |
|-------------------|---------|-------------|-------------------------------|
| `IntraPendingCount24h` | Últimas 24h | Internas sin liquidar (Estado 1-8). | `bank_intra_pending_24h_total_tx` |
| `InterPendingCount24h` | Últimas 24h | Interbancarias sin liquidar ACH (Estado 1-8). | `bank_inter_pending_24h_total_tx` |
| `IntraPendingLast7d` | Últimos 7d | Acumulado semanal de internas sin resolver. | `bank_intra_pending_7d_total_tx` |
| `InterPendingLast7d` | Últimos 7d | Acumulado semanal de interbancarias sin concretar. | `bank_inter_pending_7d_total_tx` |

### Envejecimiento del Backlog (SLA Violation)
Mide **cuántos minutos** lleva atascada la transacción más antigua dentro del bloque de pendientes de la última semana.

- `IntraPendingMaxAgeMin`: `bank_intra_pending_max_age_min_7d_min`
- `InterPendingMaxAgeMin`: `bank_inter_pending_max_age_min_7d_min`

---

## 3. Degradación, Tasa de Fallos (Calidad de Servicio)

Calcula matemáticamente qué porción del volumen total del banco se transforma en un fallo fatal (Estado 9).

| Ratio C# | Descripción | Nombre Final PromQL (Grafana) |
|----------|-------------|-------------------------------|
| `IntraFailTechRate24h` | % de fallos en transferencias Internas (últimas 24 horas). | `bank_intra_fail_tech_rate_24h_ratio` |
| `InterFailTechRate24h` | % de fallos en transferencias Interbancarias (últimas 24 horas). | `bank_inter_fail_tech_rate_24h_ratio` |
| `IntraFailTechRate30d` | Proyección de fallos interna a nivel mensual (SLA mensual). | `bank_intra_fail_tech_rate_30d_ratio` |
| `InterFailTechRate30d` | Proyección de fallos interbancaria a nivel mensual. | `bank_inter_fail_tech_rate_30d_ratio` |

### Error Share Distribution
Evalúa **qué proporción** de los estados finales en las interbancarias terminaron específicamente en error 9 (frente a los exitosos):
- `InterErrorState9Share24h` => `bank_inter_error_state9_share_24h_ratio`
- `InterErrorState9Share30d` => `bank_inter_error_state9_share_30d_ratio`

---

## 4. Cuantificación de Errores Absolutos y su Edad

Cantidades netas inyectables en paneles de Alertas Críticas (como las enviadas al celular Android FCM Bridge).

| Métrica Absoluta C# | Métrica Originaria | Nombre Final PromQL (Grafana) |
|---------------------|--------------------|-------------------------------|
| `IntraErrorCount24h` | Conteo de Fallos Internos | `bank_intra_error_24h_total_tx` |
| `InterErrorCount24h` | Conteo de Fallos Externos | `bank_inter_error_24h_total_tx` |
| `IntraErrorMaxAgeMin7d` | Edad del error interno más viejo | `bank_intra_error_max_age_min_7d_min` |
| `InterErrorMaxAgeMin7d` | Edad del error externo más viejo | `bank_inter_error_max_age_min_7d_min` |

*(Se utilizan activamente en las Alertas de Nivel Critical / Warning del sistema)*.

---

## 5. Métricas Operativas de Telemetría Interna (Health Poller)

Verificadores propios para confirmar la salud de las conexiones a la base de datos principal y ciclo de scraping:

1. `bank_sql_poller_last_poll_unixtime_s`: (Gauge) Timestamp exacto del último ciclo.
2. `bank_sql_poller_last_success_unixtime_s`: (Gauge) Timestamp exacto del último ciclo que no experimentó excepciones.
3. `bank.sqlpoller.poll_duration_sec`: (Histograma) Segundos demorados en ejecutar todo el batch `SqlQueries.*`.
4. `bank.sqlpoller.poll_errors_total`: (Counter) Sube +1 cuando el thread central estalla o corta la conexión con SQL.
