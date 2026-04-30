# Consultas y Métricas de Observabilidad (sql-poller)

El servicio `Bank.Obs.SqlPoller` ejecuta un ciclo continuo sobre la base de datos transaccional (`EconetTransacciones`) para recolectar métricas de salud operativa en tiempo real. 

Toda la instrumentación se realiza con **OpenTelemetry**. Las métricas se emiten como **Gauges** decorados con etiquetas contextuales (`source`, `estado`, `tipo`, `bucket`, `banco`, `moneda`), permitiendo un análisis multidimensional en Grafana sin sobrecargar la base de datos con agrupaciones complejas en cada consulta.

---

## 1. Volumen y Actividad (Tactical & Strategic)
Mide el flujo de entrada de transacciones en diferentes ventanas temporales. Esta visibilidad es vital para conocer el pulso transaccional de Ecofuturo.

| Ventana | Métrica OTel (Gauge) | Descripción |
|---------|----------------------|-------------|
| **5 min** | `tx_created_total_5m` | Monitoreo de actividad ultra-reciente / "Liveness". Ideal para alertar caídas bruscas. |
| **15 min** | `tx_created_total_15m` | Ritmo de entrada a corto plazo. |
| **1 hora** | `tx_created_total_1h` | Volumen táctico por hora. |
| **24 horas** | `tx_created_total_24h` | Volumen diario (línea base principal). |
| **7 / 30 días**| `tx_created_total_7d` / `tx_created_total_30d` | Tendencias históricas y estacionalidad. |

### Volumen por Atributos (Tabular)
| Indicador | Métrica OTel (Gauge) | Etiquetas |
|-----------|----------------------|-----------|
| Distribución por Estado | `tx_state_count_24h` | `source`, `estado` |
| Volumen por Tipo | `tx_type_count_24h` | `source`, `tipo` |
| Volumen por Banco (ACH) | `tx_interbank_bank_count_24h` | `banco` |

---

## 2. Gestión de Backlog (Salud del Procesamiento)
Detecta transacciones estancadas en estados intermedios (`0, 1, 6, 7, 17, 100`). Un backlog que crece y no disminuye suele ser síntoma de falla en los hilos de procesamiento (servicios de red).

| Indicador | Métrica OTel (Gauge) | Descripción |
|-----------|----------------------|-------------|
| **Backlog Total** | `tx_pending_current_total` | Cantidad absoluta de transacciones vivas pendientes en el sistema en un instante de tiempo. |
| **Backlog Reciente**| `tx_pending_recent_1h` | Transacciones creadas en la última hora que siguen pendientes. |
| **Antigüedad Máxima**| `tx_pending_oldest_seconds` | Edad en segundos de la transacción más antigua encolada sin resolución. |
| **Edad Promedio** | `tx_pending_avg_age_seconds` | Promedio de edad general del backlog, útil para dimensionar el retraso global. |
| **Edad Máxima** | `tx_pending_max_age_seconds` | Máxima edad detectada en transacciones pendientes, agrupada por `estado` y `source`. |

### Backlog Aging (Buckets)
Permite visualizar la distribución de la "deuda técnica" del procesamiento (transacciones estancadas segmentadas por tiempo):
- **Métrica:** `tx_pending_aging_bucket_count`
- **Etiquetas:** `bucket` (`ge_900s`, `ge_3600s`, `ge_14400s`), `estado`, `source`.

---

## 3. Rendimiento y SLAs (Performance)
Mide la velocidad con la que el sistema resuelve las transacciones exitosas (diferencia de tiempo en base de datos desde `fechaOperacion` hasta `fechaModificacion`).

| Indicador | Métrica OTel (Gauge) | Descripción |
|-----------|----------------------|-------------|
| **Latencia P95** | `tx_success_p95_seconds` | El 95% de las transacciones exitosas se resolvieron en este tiempo o menos. |
| **Latencia P99** | `tx_success_p99_seconds` | Medición de valores atípicos extremos (Outliers) en la latencia. |
| **Latencia Promedio**| `tx_success_avg_seconds` | Promedio general de resolución para las transacciones finalizadas exitosamente. |

---

## 4. Analítica de Negocio (Importes)
Monitoreo financiero del volumen transado, fundamental para los perfiles de riesgo y contables.

| Indicador | Métrica OTel (Gauge) | Etiquetas |
|-----------|----------------------|-----------|
| **Total Hora** | `tx_amount_total_1h` | `source` |
| **Total Diario**| `tx_amount_total_24h`| `source`, `moneda` |
| **Desagregado** | `tx_amount_by_type_24h`| `source`, `tipo`, `moneda` |
| **Por Banco** | `tx_interbank_bank_amount_total_24h`| `banco`, `moneda` |

---

## 5. Detección de Anomalías e Integridad
Métricas diseñadas para detectar fallos silenciosos, bugs de código o inconsistencias en la capa de persistencia de datos.

| Métrica OTel (Gauge) | Descripción | Escenario de Alerta |
|----------------------|-------------|---------------------|
| `tx_anomaly_zero_duration_count_24h` | TX marcadas como exitosas con exactamente 0 milisegundos de duración (mismo Timestamp de creación y modificación). | Indica procesos que podrían estar saltándose validaciones, APIs en circuito corto, o fallos de auditoría de fechas. |
| `tx_anomaly_missing_mod_count_24h` | TX cerradas/resueltas que no tienen registrada una fecha de modificación. | Error de integridad en la aplicación de core bancario o SPs incompletos. |
| `sql_poller_consecutive_failures` | Fallos de conexión a la base de datos por parte del propio servicio `SqlPoller`. | Interrupción de la visibilidad (Servicio Oculto o Base inaccesible). Dispara alertas críticas inmediatas. |

---

## 6. Errores y Degradación
Cuantificación de fallos técnicos, rechazos operativos o cancelaciones en las transferencias.

| Indicador | Métrica | Etiquetas |
|-----------|---------|-----------|
| **Rechazos** | `tx_rejected_count_24h` | `source` |
| **Fallos Técnicos** | `tx_failed_technical_count_24h` | `source` |

> [!TIP]
> **Error Rate Dinámico (Grafana):** El Error Rate relativo no se calcula de antemano en la base de datos. Se computa dinámicamente en los dashboards de Grafana utilizando Math:
> `(sum(tx_failed_technical_count_24h) / sum(tx_created_total_24h)) * 100`

---

## Glosario de Etiquetas Contextuales (Labels)
- `source`: Describe el origen de la transacción, ya sea `intra` (Transferencias Internas del mismo banco) o `inter` (ACH/Interbancarias/Cámaras de Compensación).
- `estado`: ID numérico del estado de la transacción según el manual de base de datos (Ej: 3=Éxito, 1=Pendiente, 2=Rechazado).
- `tipo`: Tipo de transacción operativa (Ej: 1=Normal, 2=Programada).
- `banco`: ID del banco destino asociado al catálogo de ACH (Solo aplica para `source = inter`).
- `moneda`: ID de la moneda transada (0=BOB Bolivianos, 1=USD Dólares, etc.).
- `bucket`: Ventana de tiempo agrupadora para el SLA (Ej: `ge_900s` = greater or equal to 15 minutos).
