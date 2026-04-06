# Consultas y Métricas de Observabilidad (sql-poller)

El servicio `Bank.Obs.SqlPoller` ejecuta un ciclo continuo (cada 1 minuto) sobre la base de datos transaccional (`EconetTransacciones` de SQL Server) para recolectar métricas de salud operativa en tiempo real: volumen, estado de retención y fallos.

Toda la instrumentación se realiza nativamente con **OpenTelemetry en .NET 9**. Las consultas emiten hechos absolutos (Base Metrics) sin realizar agrupaciones analíticas pesadas. La analítica compleja, cálculos de ratios del negocio y el procesamiento de alertas se delegan a PromQL y a Grafana.

---

## 1. Volumen Transaccional (TX Creadas)
Mide la cantidad neta de operaciones que entraron al sistema para ser transferidas (tanto las que tuvieron éxito como la que fallaron).

**Tablas fuente:** `Transferencia` (internas) y `TransferenciaInterbancaria` (interbancarias ACH).

| Ventana | Descripción | Métrica OTel (Gauge) | Etiqueta `source` |
|---------|-------------|----------------------|-------------------|
| Últimos 15 min | Operaciones recientes para monitoreo de actividad vivo ultrarrápida. | `tx_created_total_15m` | `intra` / `inter` |
| Últimas 24h | Operaciones diarias (fundamental para promedios baselines y alarmas de volúmenes). | `tx_created_total_24h` | `intra` / `inter` |
| Últimos 7d | Volumen total operado en la última ventana semanal corrida. | `tx_created_total_7d` | `intra` / `inter` |
| Últimos 30d | Operaciones mensuales para analíticas de rendimiento. | `tx_created_total_30d` | `intra` / `inter` |

*Ejemplo Query SQL Base:* 
```sql
SELECT COUNT(1) FROM Transferencia 
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE())
```

---

## 2. Transacciones Pendientes (Backlog Estancado)
Monitoriza las transacciones que fueron creadas pero que el motor bancario todavía **no ha dictaminado con éxito ni cerrado en un estado final de falla**. 

**Estados contemplados (Intra & Inter):** `0, 1, 6, 7, 17, 100` (Creado, En proceso, Pendiente Confirmación, etc.).

| Indicador | Métrica OTel (Gauge) | Descripción |
|-----------|----------------------|-------------|
| Estancadas 24h | `tx_pending_count_24h` | Cantidad total de transacciones sin liquidar originadas el último día. |
| Estancadas 7d | `tx_pending_count_7d`  | Total de retención extendida, útil para evidenciar colapso asíncrono residual. |
| Antigüedad | `tx_pending_oldest_seconds` | La edad (en segundos) de la transacción estancada **más vieja**. Es crítico para configurar las alertas SLA de retenciones, si pasa de 300 segundos disparamos notificación de Backlog crítico. |

*Ejemplo Query SQL (Envejecimiento):* 
```sql
SELECT ISNULL(DATEDIFF(second, MIN(fechaOperacion), GETDATE()), 0) 
FROM TransferenciaInterbancaria 
WHERE estado IN (0,1,6,7,17,100)
```

---

## 3. Degradación y Errores Técnicos
Cuantifica de forma estricta los fallos técnicos terminales o rechazos comerciales bloqueantes de la jornada operativa en curso.

**Estados de error:** `2, 4, 5, 15` (Rechazos formales, Fallos de Red, Timed Out).

| Indicador | Métrica OTel (Gauge) | Descripción |
|-----------|----------------------|-------------|
| Fallos Diarios | `tx_error_count_24h` | Total acumulado de transferencias finalizadas en estado de error explícito (últimas 24h). |

> **Transición de Arquitectura (Nube a PromQL):** A diferencia de arquitecturas previas monolíticas, el código del `sql-poller` en .NET ya no calcula el "Error Rate" internamente (eso consumía recursos en la DB para dividir fracciones). Ahora exporta los crudos, y esa conversión métrica se hace evaluando en Grafana con PromQL de la siguiente forma:
> `sum(tx_error_count_24h) / sum(tx_created_total_24h) * 100`

---

## 4. Ritmo de Resoluciones y Cierres (Throughput Speed)
Analiza la velocidad general en la que el sistema resuelve y asienta de baja los flujos, demostrando que tan despejada se encuentra nuestra cola ACH o Core.

**Estados resueltos:** `3, 8, 9` (Liquidado con confirmación, Extornado explícitamente, Finalización con Excepción catalogada).

| Indicador | Métrica OTel (Gauge) | Descripción |
|-----------|----------------------|-------------|
| Cantidad Resuelta | `tx_resolved_count_24h` | Total absoluto de transacciones cerradas de la máquina del estado. |
| Rapidez Resolutiva | `tx_resolution_avg_seconds` | Promedio en segundos de la diferencia temporal detectada entre `fechaOperacion` nativa y la `fechaModificacion` final de resolución en la tabla. |

*Ejemplo Query SQL (Tiempo de Cierre Avg):* 
```sql
SELECT ISNULL(AVG(DATEDIFF(second, fechaOperacion, ISNULL(fechaModificacion, GETDATE()))), 0) 
FROM Transferencia WHERE estado IN (3,8,9) AND fechaOperacion >= DATEADD(hour, -24, GETDATE())
```

---

## 5. Métricas de Salud Operativa (Observabilidad del Propio Poller)
Métricas internas exclusivas sobre el mismo servicio de telemetría de Ecofuturo `Bank.Obs.SqlPoller`. 

| Tipo de Métrica | Nombre Exposición OTel | Descripción de Vida |
|-----------------|------------------------|---------------------|
| **Gauge** | `sql_poller_last_success_timestamp` | Timestamp *UnixEpoch* nativo del último acceso que resolvió todo el macro-ciclo de Data Readers exitosamente. Dispara un alerta de Poller Muerto si su antiguedad sobrepasa los 5 minutos. |
| **Gauge** | `sql_poller_consecutive_failures` | Contador punitivo que se eleva ante timeout de SQL / DB Offline. Cae a `0` apenas el servicio restaura conexión real. |
| **Histogram (ms)**| `sql_poller_cycle_duration_seconds` | Latencia. Segundos completos empleados en mandar las sentencias SQL y obtener los resultados en memoria RAM (mide la degradación de recursos). |
| **Counter (inc)** | `sql_poller_errors_total` | Monotónico incremental puro para análisis de paradas a largo plazo. |

> **Desagregación:** Nota que todas las métricas de negocio exportadas via OpenTelemetry están "decoradas" dinámicamente con la metadata contextual: el pair `source="intra"` e intermitentemente `source="inter"`, lo que permite multiplexar los gráficos de Grafana de un solo golpe.
