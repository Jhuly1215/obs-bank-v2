# Consultas y Métricas de Observabilidad (sql-poller)

El servicio `Bank.Obs.SqlPoller` ejecuta un ciclo continuo sobre la base de datos transaccional (`EconetTransacciones`) para recolectar métricas de salud operativa en tiempo real. 

Toda la instrumentación se realiza con **OpenTelemetry**. Las métricas se emiten como **Gauges** decorados con etiquetas contextuales (`source`, `estado`, `tipo`, `bucket`, `banco`, `moneda`), permitiendo un análisis multidimensional en Grafana sin sobrecargar la base de datos con agrupaciones complejas en cada consulta.

---

## 1. Volumen y Actividad (Tactical & Strategic)
Mide el flujo de entrada de transacciones en diferentes ventanas temporales.

| Ventana | Métrica OTel (Gauge) | Descripción |
|---------|----------------------|-------------|
| **5 min** | `tx_created_total_5m` | **(Nuevo)** Monitoreo de actividad ultra-reciente / "Liveness". |
| **15 min** | `tx_created_total_15m` | Ritmo de entrada a corto plazo. |
| **1 hora** | `tx_created_total_1h` | **(Nuevo)** Volumen táctico por hora. |
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
Detecta transacciones estancadas en estados intermedios (`0, 1, 6, 7, 17, 100`).

| Indicador | Métrica OTel (Gauge) | Descripción |
|-----------|----------------------|-------------|
| **Backlog Total** | `tx_pending_current_total` | **(Nuevo)** Cantidad absoluta de transacciones vivas en el sistema. |
| **Backlog Reciente**| `tx_pending_recent_1h` | **(Nuevo)** Transacciones creadas en la última hora que siguen pendientes. |
| **Antigüedad Máxima**| `tx_pending_oldest_seconds` | Edad en segundos de la transacción más antigua. |
| **Edad Promedio** | `tx_pending_avg_age_seconds` | Promedio de edad agrupado por `estado`. |
| **Edad Máxima** | `tx_pending_max_age_seconds` | Máxima edad detectada agrupada por `estado`. |

### Backlog Aging (Buckets)
Permite visualizar la distribución de la "deuda técnica" del procesamiento:
- **Métrica:** `tx_pending_aging_bucket_count`
- **Etiquetas:** `bucket` (`ge_900s`, `ge_3600s`, `ge_14400s`), `estado`, `source`.

---

## 3. Rendimiento y SLAs (Performance)
Mide la velocidad con la que el sistema resuelve las transacciones exitosas (desde `fechaOperacion` a `fechaModificacion`).

| Indicador | Métrica OTel (Gauge) | Descripción |
|-----------|----------------------|-------------|
| **Latencia P95** | `tx_success_p95_seconds` | El 95% de las TX se resuelven en este tiempo o menos. |
| **Latencia P99** | `tx_success_p99_seconds` | **(Nuevo)** Medición de valores atípicos (Outliers) extremos. |
| **Latencia Promedio**| `tx_success_avg_seconds` | Promedio general de resolución. |

---

## 4. Analítica de Negocio (Importes)
Monitoreo financiero del volumen transado.

| Indicador | Métrica OTel (Gauge) | Etiquetas |
|-----------|----------------------|-----------|
| **Total Hora** | `tx_amount_total_1h` | `source` |
| **Total Diario**| `tx_amount_total_24h`| `source`, `moneda` |
| **Desagregado** | `tx_amount_by_type_24h`| `source`, `tipo`, `moneda` |
| **Por Banco** | `tx_interbank_bank_amount_total_24h`| `banco`, `moneda` |

---

## 5. Detección de Anomalías e Integridad
Métricas diseñadas para detectar fallos silenciosos o inconsistencias en los datos.

| Métrica OTel (Gauge) | Descripción | Escenario de Alerta |
|----------------------|-------------|---------------------|
| `tx_anomaly_zero_duration_count_24h` | **(Nuevo)** TX marcadas como exitosas con 0 segundos de duración. | Indica procesos que podrían estar saltándose validaciones o fallos de auditoría. |
| `tx_anomaly_missing_mod_count_24h` | **(Nuevo)** TX cerradas sin fecha de modificación. | Error de integridad en el Motor de Base de Datos. |
| `sql_poller_consecutive_failures` | Fallos de conexión del propio servicio. | Interrupción de la visibilidad (Servicio Muerto). |

---

## 6. Errores y Degradación
Cuantificación de fallos técnicos y rechazos.

| Indicador | Métrica | Etiquetas |
|-----------|---------|-----------|
| **Rechazos** | `tx_rejected_count_24h` | `source` |
| **Fallos Técnicos** | `tx_failed_technical_count_24h` | `source` |

> [!TIP]
> **Error Rate Dinámico:** No se calcula en la base de datos. Se calcula en Grafana usando:
> `(sum(tx_failed_technical_count_24h) / sum(tx_created_total_24h)) * 100`

---

## Glosario de Etiquetas Contextuales
- `source`: `intra` (Transferencias Internas) o `inter` (ACH/Interbancarias).
- `estado`: ID numérico del estado en la base de datos (Ej: 3=Éxito, 1=Pendiente).
- `tipo`: Tipo de transacción (Ej: 1=Normal, 2=Programada).
- `banco`: ID del banco destino (Solo para `inter`).
- `moneda`: ID de la moneda (0=BOB, 1=USD).
