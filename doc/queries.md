# Consultas y Métricas de Observabilidad (sql-poller)

El servicio `Bank.Obs.SqlPoller` ejecuta un ciclo (*polling*) continuo sobre la base de datos de Transacciones para recolectar indicadores operativos (Volumen, Pendientes y Errores). 

A continuación, se detalla **qué hace cada consulta SQL** (`SqlQueries.cs`) y **a qué métrica de OpenTelemetry** (`SqlMetrics.cs`) alimenta.

---

## 1. Volumen Transaccional (Conteos)

Se mide la cantidad neta de operaciones que llegaron al sistema, tanto en la tabla interna (`Transferencia`) como interbancaria (`TransferenciaInterbancaria`).

| Consulta (Query) | Ventana (Tiempo) | Descripción Exacta | Nombre de Métrica OTel |
|------------------|------------------|---------------------|------------------------|
| `IntraTxLast15m` | Últimos 15 min | Cantidad de `Transferencia` creadas en los últimos 15 min. | `bank.transfer.intra.last15m` |
| `InterTxLast15m` | Últimos 15 min | Cantidad de `TransferenciaInterbancaria` creadas en los últimos 15 min. | `bank.transfer.inter.last15m` |
| `IntraTxLast24h` | Últimas 24 horas | Cantidad total de transferencias internas (`Transferencia`) recibidas durante las últimas 24 hrs. | `bank.transfer.intra.last24h` |
| `InterTxLast24h` | Últimas 24 horas | Cantidad total de transferencias interbancarias recibidas durante las últimas 24 hrs. | `bank.transfer.inter.last24h` |
| `IntraTxLast30d` | Últimos 30 días | Rendimiento de volumen mensual (interno). | `bank.transfer.intra.last30d` |
| `InterTxLast30d` | Últimos 30 días | Rendimiento de volumen mensual (interbancario). | `bank.transfer.inter.last30d` |

*(Nota: Estas métricas son del tipo `ObservableGauge`. Grafana las recibe como el conteo bruto que había en caja negra en el momento del scrape).*

---

## 2. Backlog y Pendientes

Registra operaciones que se encuentran "atrapadas" en un estado intermedio (estados `1` a `8`). Según la lógica de negocio, **el estado 9 representa un Error técnico/rechazo, por lo que NO se contabiliza como pendiente, sino como error**.

| Consulta (Query) | Ventana | Descripción Exacta | Nombre de Métrica OTel |
|------------------|----------|---------------------|------------------------|
| `IntraPendingCount24h` | Últimas 24h | Cuenta interna de transferencias cuyo *EstadoId* sea entre 1 y 8. | `bank.transfer.intra.pending_24h` |
| `InterPendingCount24h` | Últimas 24h | Cuenta interbancaria de transferencias con *EstadoId* entre 1 y 8. | `bank.transfer.inter.pending_24h` |
| `IntraPendingLast7d` | Últimos 7 días| Acumulado de internas que no hayan resuelto en toda la semana. | `bank.transfer.intra.pending_7d` |
| `InterPendingLast7d` | Últimos 7 días| Acumulado de interbancarias sin concretar en la semana. | `bank.transfer.inter.pending_7d` |

### Envejecimiento de Pendientes (Max Age)
El poller también mide **cuántos minutos lleva trabada** la transacción pendiente más antigua. Esto lanza alarmas de SLA (Service Level Agreement).
- `IntraPendingMaxAgeMin`: `bank.transfer.intra.pending_maxage_min`
- `InterPendingMaxAgeMin`: `bank.transfer.inter.pending_maxage_min`

*(Técnicamente hace un `DATEDIFF(MINUTE, MIN(FechaCreacion), GETDATE())` de las transacciones en estado 1-8).*

---

## 3. Calidad y Ratio de Errores (Fail Rates)

Mide la porción probabilística (ratio) de las operaciones que terminan en un fallo técnico (Estado `9`). 

| Consulta / Métrica OTel | Descripción de la Consulta SQL |
|--------------------------|---------------------------------|
| `bank.transfer.intra.fail_rate_24h` (`IntraFailTechRate24h`) | Calcula `(Totales_con_Estado9 * 100.0) / Totales_General` en las últimas 24 hrs (Internas). |
| `bank.transfer.inter.fail_rate_24h` (`InterFailTechRate24h`) | Calcula la misma tasa de fallos, pero de Interbancarias en 24h. |
| `bank.transfer.intra.fail_rate_30d` / `.inter.fail_rate_30d` | El mismo ratio matemático de calidad pero proyectado sobre la ventana de 30 días. |

### Share (Distribución de Errores)
Para las interbancarias, se incluye un Ratio particular (`InterErrorState9Share24h` / `bank.transfer.inter.state9_share_24h`) que evalúa **qué porcentaje** representa el estado 9 sobre TODOS los estados que son considerados "estados finales / observados recurrentemente" en un día de fallo masivo.

---

## 4. Conteo de Errores y Aging (Errores en el tiempo)

Cantidad absoluta de errores (Estado = 9) y qué tan viejos son.

- **Conteo Puro (24h):**
  - Internas (`IntraErrorCount24h` / `bank.transfer.intra.error_count_24h`).
  - Interbancarias (`InterErrorCount24h` / `bank.transfer.inter.error_count_24h`).

- **Antigüedad Máxima del Error Prominente (7 días):**
  - Mide para determinar *hace cuánto tiempo explotó* la transacción truncada más vieja de la semana.
  - OTel: `bank.transfer.intra.error_maxage_min_7d` / `bank.transfer.inter.error_maxage_min_7d`.

---

## 5. Métricas del Propio Poller (Salud del Worker)

El proceso mismo expone dos métricas adicionales para auditar si el ciclo de ejecución está funcionando correctamente:

1. `bank.sqlpoller.poll_duration_sec`: *Histograma* que registra cuántos segundos tarda el batch entero de las consultas a SQL en terminar de forma exitosa.
2. `bank.sqlpoller.poll_errors_total`: *Counter* que sube +1 cada vez que un ciclo de polling entero revienta (ej: por pérdida de conexión TCP con SQL Server).

---
*Documentación generada técnica acorde a la versión v2 del Observability Stack.*
