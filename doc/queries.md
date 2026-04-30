# Consultas SQL del SqlPoller en ObsBank-v2

Este documento describe el estado actual de las consultas SQL utilizadas por el servicio `Bank.Obs.SqlPoller` en la rama `base`.

La documentación se basa en la carpeta real:

```text
services/Bank.Obs.SqlPoller/Bank.Obs.SqlPoller/Polling/Queries/
```

Actualmente las consultas están definidas como constantes C# dentro de una clase parcial:

```csharp
namespace Bank.Obs.SqlPoller.Polling;

public static partial class SqlQueries
```

La carpeta `Queries` contiene tres archivos:

```text
Queries/
├── SqlQueries.Health.cs
├── SqlQueries.Intra.cs
└── SqlQueries.Inter.cs
```

---

## 1. Resumen general

El SqlPoller consulta dos tablas transaccionales principales:

| Archivo | Tabla principal | Propósito |
|---|---|---|
| `SqlQueries.Intra.cs` | `Transferencia` | Métricas de transferencias intrabancarias. |
| `SqlQueries.Inter.cs` | `TransferenciaInterbancaria` | Métricas de transferencias interbancarias. |
| `SqlQueries.Health.cs` | DMVs SQL Server + ambas tablas | Estado básico del día, salud de base de datos y anomalías de calidad de datos. |

La estrategia actual separa las consultas en tres grupos:

1. **Consultas de negocio intra**: volumen, estados, montos, pendientes, errores, latencias y anomalías sobre `Transferencia`.
2. **Consultas de negocio inter**: mismas métricas generales sobre `TransferenciaInterbancaria`, más métricas por banco destino.
3. **Consultas de salud/calidad**: tipo de día, sesiones, I/O, tamaño de base y anomalías cruzadas entre intra e inter.

---

## 2. Taxonomía de estados

La taxonomía de estados está definida en `SqlQueries.Intra.cs` y es reutilizada por las consultas intra e inter.

```csharp
public const string ProgrammedStates = "0,7,17";
public const string OpPendingStates = "1,6";
public const string ReviewStates = "100";
public const string RejectedStates = "2,4";
public const string TechFailedStates = "5,15";
public const string CompensatedStates = "9";
public const string SuccessStates = "3,8";
public const string AllPendingStates = "0,1,6,7,17,100";
```

| Grupo | Estados | Interpretación actual en el código |
|---|---:|---|
| `ProgrammedStates` | `0, 7, 17` | Operaciones programadas o no operativas inmediatas. |
| `OpPendingStates` | `1, 6` | Pendientes operativos reales. |
| `ReviewStates` | `100` | Operaciones en revisión. |
| `RejectedStates` | `2, 4` | Rechazos. |
| `TechFailedStates` | `5, 15` | Fallas técnicas. |
| `CompensatedStates` | `9` | Compensadas. |
| `SuccessStates` | `3, 8` | Exitosas o cerradas limpiamente según comentario del código. |
| `AllPendingStates` | `0, 1, 6, 7, 17, 100` | Conjunto amplio para compatibilidad con dashboards antiguos. |

Punto importante: no todos los pendientes son iguales. El código ya diferencia entre pendientes operativos (`1,6`), programados (`0,7,17`) y revisión (`100`). Esta separación es correcta porque mezclar programados con pendientes operativos puede inflar falsamente la percepción de cola o incidente.

---

## 3. Convenciones actuales de ventanas de tiempo

Las consultas usan ventanas relativas calculadas con `GETDATE()`:

| Ventana | Uso |
|---|---|
| 5 minutos | Volumen táctico reciente. |
| 15 minutos | Métrica base histórica compatible con dashboards previos. |
| 1 hora | Volumen y monto reciente. |
| 24 horas | Ventana principal para conteos, estados, montos, errores, cierres y latencias. |
| 7 días | Conteos históricos de volumen y pendientes. |
| 30 días | Conteos históricos de volumen. |

El propio `SqlPollingClient` marca las consultas de 7 y 30 días como más costosas y recomienda moverlas a análisis offline si generan carga.

---

## 4. Consultas de `SqlQueries.Intra.cs`

Estas consultas trabajan sobre la tabla:

```sql
Transferencia
```

La tabla representa transferencias intrabancarias.

### 4.1. Distribución por estado

| Constante | Ventana | Resultado | Uso |
|---|---|---|---|
| `IntraStateCount24h` | 24h | `estado`, `Count` | Cuenta operaciones por estado durante las últimas 24 horas. |

Consulta base:

```sql
SELECT estado, COUNT(1) AS [Count]
FROM Transferencia
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE())
GROUP BY estado
```

Sirve para dashboards de distribución general por estado.

---

### 4.2. Pendientes operativos y programados

| Constante | Estados | Resultado | Uso |
|---|---:|---|---|
| `IntraOpPendingCount` | `1,6` | `estado`, `Count` | Backlog operativo real. |
| `IntraProgrammedCount` | `0,7,17` | `estado`, `Count` | Backlog programado. |
| `IntraPendingCount24h` | `1,6,100` | total | Pendientes operativos y revisión en 24h. |
| `IntraPendingCount7d` | `1,6,100` | total | Pendientes operativos y revisión en 7 días. |
| `IntraPendingOldestSeconds` | `1,6,100` | segundos | Edad del pendiente más antiguo. |

Estas consultas separan lo que realmente requiere atención operativa de lo que solo está programado.

Criterio actual:

```sql
estado IN ({OpPendingStates}, {ReviewStates})
```

para pendientes operativos usados en dashboards.

---

### 4.3. Estado 100 / revisión

| Constante | Estado | Resultado | Uso |
|---|---:|---|---|
| `IntraReviewStats` | `100` | `TotalCount`, `AvgSec`, `MaxSec`, `DeadCount` | Métricas específicas para operaciones en revisión. |

La consulta calcula:

- Total de operaciones en revisión.
- Edad promedio en segundos.
- Edad máxima en segundos.
- Cantidad considerada crítica si supera 300 segundos.

Criterio de dead/revisión crítica:

```sql
DATEDIFF(second, fechaOperacion, GETDATE()) >= 300
```

Esto equivale a 5 minutos.

---

### 4.4. Aging granular de pendientes

| Constante | Estados | Buckets | Uso |
|---|---:|---|---|
| `IntraPendingAgingBucketCount` | `1,6,100` | `>=900s`, `>=3600s`, `>=14400s` | Clasifica pendientes por antigüedad. |
| `IntraPendingAgeStats` | `1,6,100` | `AvgSec`, `MaxSec` | Promedio y máximo por estado. |

Los buckets equivalen a:

| Campo | Tiempo |
|---|---:|
| `Ge900s` | 15 minutos o más |
| `Ge3600s` | 1 hora o más |
| `Ge14400s` | 4 horas o más |

Este bloque es útil para alertas de saturación o envejecimiento de cola.

---

### 4.5. Fallas, rechazos y compensadas

| Constante | Estados | Resultado | Uso |
|---|---:|---|---|
| `IntraFailures24h` | `2,4`, `5,15`, `9` | `Rejected`, `FailedTechnical`, `Compensated` | Resume fallas y cierres no exitosos de las últimas 24h. |
| `IntraErrorCount24h` | `2,4`, `5,15` | total | Total de errores/rechazos en 24h. |
| `IntraResolvedCount24h` | `3,8,9` | total | Operaciones resueltas en 24h. |
| `IntraCompensatedCurrentCount` | `9` | total | Total actual de compensadas con `fechaOperacion`. |

La consulta `IntraFailures24h` diferencia tres grupos:

```text
Rejected        -> estados 2,4
FailedTechnical -> estados 5,15
Compensated     -> estado 9
```

Esto evita tratar todas las salidas no exitosas como un mismo tipo de error.

---

### 4.6. Volumen por tipo y ventanas tácticas

| Constante | Ventana | Resultado |
|---|---|---|
| `IntraTypeCount24h` | 24h | `Tipo`, `Count` |
| `IntraTxCreated5m` | 5m | total |
| `IntraTxCreated15m` | 15m | total |
| `IntraTxCreated1h` | 1h | total |
| `IntraTxCreated24h` | 24h | total |
| `IntraTxCreated7d` | 7d | total |
| `IntraTxCreated30d` | 30d | total |

La consulta por tipo agrupa usando:

```sql
ISNULL(CAST(tipoTransferencia AS INT), 0) AS Tipo
```

Esto convierte valores nulos a `0`.

---

### 4.7. Montos

| Constante | Ventana | Agrupación | Resultado |
|---|---|---|---|
| `IntraAmountByType24h` | 24h | `tipoTransferencia`, `codigoMoneda` | `Tipo`, `Moneda`, `Total` |
| `IntraAmountTotal24h` | 24h | `codigoMoneda` | `Moneda`, `Total` |
| `IntraAmountTotal1h` | 1h | sin agrupación | total |

Para intra, la moneda se obtiene desde:

```sql
codigoMoneda
```

No se debe documentar como `monedaOperacion`, porque eso corresponde a interbancarias.

---

### 4.8. Latencia de operaciones exitosas

| Constante | Estados | Resultado | Uso |
|---|---:|---|---|
| `IntraSuccessSpeed24h` | `3,8` | `Tipo`, `AvgSec`, `P95Sec` | Latencia promedio y percentil 95 por tipo. |
| `IntraSuccessSpeedP99_24h` | `3,8` | total P99 | Percentil 99 general. |
| `IntraResolutionAvgSeconds` | `3,8` | promedio | Promedio general de resolución en 24h. |

La latencia se calcula con:

```sql
DATEDIFF(second, fechaOperacion, fechaModificacion)
```

Solo se toman operaciones con `fechaOperacion` y `fechaModificacion` no nulas.

Los percentiles usan:

```sql
PERCENTILE_CONT(0.95)
PERCENTILE_CONT(0.99)
```

Estas consultas pueden ser más costosas que simples conteos porque requieren ordenar duraciones.

---

### 4.9. Calidad de datos y anomalías intra

| Constante | Resultado | Uso |
|---|---|---|
| `IntraZeroDurationCount24h` | total | Cuenta éxitos con duración 0 segundos. |
| `IntraMissingModificationCount24h` | total | Cuenta éxitos sin `fechaModificacion`. |
| `IntraSuccessCount24h` | total | Total de operaciones exitosas en 24h. |
| `IntraClosedCount24h` | total | Total de operaciones cerradas por éxito, compensación, rechazo o falla técnica. |
| `IntraOtherStateCount24h` | total | Estados no clasificados por la taxonomía actual. |

La consulta de estados no clasificados es importante para detectar cambios de negocio o estados nuevos que el dashboard aún no contempla.

---

## 5. Consultas de `SqlQueries.Inter.cs`

Estas consultas trabajan sobre la tabla:

```sql
TransferenciaInterbancaria
```

La lógica es paralela a intra, pero tiene diferencias de campos:

| Concepto | Intra | Inter |
|---|---|---|
| Tabla | `Transferencia` | `TransferenciaInterbancaria` |
| Moneda | `codigoMoneda` | `monedaOperacion` |
| Banco destino | No aplica | `bancoDestino` |

---

### 5.1. Distribución por estado

| Constante | Ventana | Resultado | Uso |
|---|---|---|---|
| `InterStateCount24h` | 24h | `estado`, `Count` | Cuenta interbancarias por estado en las últimas 24h. |

---

### 5.2. Pendientes operativos y programados

| Constante | Estados | Resultado | Uso |
|---|---:|---|---|
| `InterOpPendingCount` | `1,6` | `estado`, `Count` | Backlog operativo real interbancario. |
| `InterProgrammedCount` | `0,7,17` | `estado`, `Count` | Backlog programado interbancario. |
| `InterPendingCount24h` | `1,6,100` | total | Pendientes operativos y revisión en 24h. |
| `InterPendingCount7d` | `1,6,100` | total | Pendientes operativos y revisión en 7 días. |
| `InterPendingOldestSeconds` | `1,6,100` | segundos | Edad del pendiente inter más antiguo. |

---

### 5.3. Estado 100 / revisión ACH

| Constante | Estado | Resultado | Uso |
|---|---:|---|---|
| `InterReviewStats` | `100` | `TotalCount`, `AvgSec`, `MaxSec`, `DeadCount` | Métricas críticas para revisión interbancaria/ACH. |

También usa 300 segundos como umbral de revisión crítica.

---

### 5.4. Aging granular de pendientes

| Constante | Estados | Buckets | Uso |
|---|---:|---|---|
| `InterPendingAgingBucketCount` | `1,6,100` | `>=900s`, `>=3600s`, `>=14400s` | Clasifica pendientes interbancarios por antigüedad. |
| `InterPendingAgeStats` | `1,6,100` | `AvgSec`, `MaxSec` | Edad promedio y máxima por estado. |

---

### 5.5. Fallas, rechazos y compensadas

| Constante | Estados | Resultado | Uso |
|---|---:|---|---|
| `InterFailures24h` | `2,4`, `5,15`, `9` | `Rejected`, `FailedTechnical`, `Compensated` | Fallas, rechazos y compensadas en 24h. |
| `InterErrorCount24h` | `2,4`, `5,15` | total | Errores/rechazos en 24h. |
| `InterResolvedCount24h` | `3,8,9` | total | Resueltas en 24h. |
| `InterCompensatedCurrentCount` | `9` | total | Compensadas actuales con `fechaOperacion`. |

---

### 5.6. Volumen por tipo y ventanas tácticas

| Constante | Ventana | Resultado |
|---|---|---|
| `InterTypeCount24h` | 24h | `Tipo`, `Count` |
| `InterTxCreated5m` | 5m | total |
| `InterTxCreated15m` | 15m | total |
| `InterTxCreated1h` | 1h | total |
| `InterTxCreated24h` | 24h | total |
| `InterTxCreated7d` | 7d | total |
| `InterTxCreated30d` | 30d | total |

---

### 5.7. Montos

| Constante | Ventana | Agrupación | Resultado |
|---|---|---|---|
| `InterAmountByType24h` | 24h | `tipoTransferencia`, `monedaOperacion` | `Tipo`, `Moneda`, `Total` |
| `InterAmountTotal24h` | 24h | `monedaOperacion` | `Moneda`, `Total` |
| `InterAmountTotal1h` | 1h | sin agrupación | total |

Para interbancarias, la moneda se obtiene desde:

```sql
monedaOperacion
```

---

### 5.8. Latencia de operaciones exitosas

| Constante | Estados | Resultado | Uso |
|---|---:|---|---|
| `InterSuccessSpeed24h` | `3,8` | `Tipo`, `AvgSec`, `P95Sec` | Latencia promedio y P95 por tipo. |
| `InterSuccessSpeedP99_24h` | `3,8` | total P99 | Percentil 99 general. |
| `InterResolutionAvgSeconds` | `3,8` | promedio | Promedio general de resolución en 24h. |

La latencia usa:

```sql
DATEDIFF(second, fechaOperacion, fechaModificacion)
```

---

### 5.9. Métricas por banco destino

Estas consultas existen solo para interbancarias.

| Constante | Ventana | Agrupación | Resultado |
|---|---|---|---|
| `InterBankCount24h` | 24h | `bancoDestino` | `Banco`, `Count` |
| `InterBankStateCount24h` | 24h | `bancoDestino`, `estado` | `Banco`, `estado`, `Count` |
| `InterBankAmountTotal24h` | 24h | `bancoDestino`, `monedaOperacion` | `Banco`, `Moneda`, `Total` |

Estas métricas permiten detectar concentración de volumen, errores o montos por banco destino.

---

### 5.10. Calidad de datos y anomalías inter

| Constante | Resultado | Uso |
|---|---|---|
| `InterZeroDurationCount24h` | total | Cuenta éxitos con duración 0 segundos. |
| `InterMissingModificationCount24h` | total | Cuenta éxitos sin `fechaModificacion`. |
| `InterSuccessCount24h` | total | Total de interbancarias exitosas en 24h. |
| `InterClosedCount24h` | total | Total de interbancarias cerradas. |
| `InterOtherStateCount24h` | total | Estados no clasificados por la taxonomía actual. |

---

## 6. Consultas de `SqlQueries.Health.cs`

Este archivo mezcla contexto operativo, salud técnica y calidad de datos.

### 6.1. Tipo de día

| Constante | Resultado | Uso |
|---|---|---|
| `GetDayType` | `habil` o `fin_de_semana` | Clasifica el día actual para dashboards o alertas. |

La consulta evita depender directamente de `@@DATEFIRST` porque calcula domingo y sábado usando fechas de referencia:

```sql
DECLARE @Sunday INT = DATEPART(dw, '2023-01-01');
DECLARE @Saturday INT = DATEPART(dw, '2023-01-07');
```

Luego compara el día actual contra esos valores.

---

### 6.2. Sesiones y esperas SQL Server

| Constante | Fuente | Resultado |
|---|---|---|
| `ServerSessions` | `sys.dm_exec_requests` | `Status`, `WaitType`, `Count` |

Consulta sesiones activas con `session_id > 50`, agrupadas por estado y tipo de espera.

Punto crítico: en `SqlPollingClient` esta consulta está comentada por falta de permisos `VIEW SERVER STATE`.

---

### 6.3. I/O real de archivos

| Constante | Fuente | Resultado |
|---|---|---|
| `FileIoStats` | `sys.dm_io_virtual_file_stats(DB_ID(), NULL)` | `FileId`, `ReadStallMs`, `WriteStallMs`, `BytesRead`, `BytesWritten` |

Sirve para medir espera de lectura/escritura y volumen de bytes por archivo lógico/físico.

Punto crítico: también está comentada actualmente por permisos.

---

### 6.4. Tamaño y uso de archivos de base de datos

| Constante | Fuente | Resultado |
|---|---|---|
| `DatabaseSize` | `sys.database_files` | `FileName`, `SizeMB`, `UsedMB` |

Calcula tamaño asignado y espacio usado en MB.

Punto crítico: igual que las anteriores, la ejecución está comentada en el cliente actual.

---

### 6.5. Anomalías de calidad de datos

| Constante | Tablas | Resultado |
|---|---|---|
| `QualityAnomalies` | `Transferencia` + `TransferenciaInterbancaria` | `Source`, `Tipo`, `Estado`, `MissingMod`, `ZeroDur`, `NegDur` |

Esta consulta une intra e inter de las últimas 24h usando `UNION ALL`.

Detecta tres anomalías:

| Campo | Condición | Significado |
|---|---|---|
| `MissingMod` | `fechaModificacion IS NULL AND estado IN (3,8)` | Operación exitosa/cerrada sin fecha de modificación. |
| `ZeroDur` | `DATEDIFF(second, fechaOperacion, fechaModificacion) = 0 AND estado IN (3,8)` | Duración cero. |
| `NegDur` | `DATEDIFF(second, fechaOperacion, fechaModificacion) < 0 AND estado IN (3,8)` | Fecha de modificación anterior a la operación. |

Esta consulta sí se ejecuta actualmente en `SqlPollingClient`.

---

## 7. Consultas ejecutadas actualmente por `SqlPollingClient`

El cliente `SqlPollingClient` ejecuta las consultas en cada ciclo de polling y construye un `Snapshot`.

### 7.1. Consultas ejecutadas

Se ejecutan consultas de:

- Tipo de día.
- Volumen intra/inter en 5m, 15m, 1h, 24h, 7d y 30d.
- Pendientes intra/inter en 24h y 7d.
- Pendiente más antiguo.
- Errores, resueltas y cerradas.
- Distribución por estado.
- Backlog operativo vs programado.
- Revisión estado 100.
- Aging buckets.
- Fallas, rechazos y compensadas.
- Volumen por tipo.
- Monto por moneda y por tipo.
- Latencia promedio, P95 y P99.
- Métricas por banco destino para interbancarias.
- Anomalías de calidad de datos.

### 7.2. Consultas definidas pero no ejecutadas actualmente

Estas consultas existen en `SqlQueries.Health.cs`, pero están comentadas en el cliente:

| Consulta | Motivo indicado en código |
|---|---|
| `ServerSessions` | Falta de permisos `VIEW SERVER STATE`. |
| `FileIoStats` | Falta de permisos `VIEW SERVER STATE`. |
| `DatabaseSize` | Incluida en el bloque comentado de health/resources. |

El código asigna arreglos vacíos para estas métricas:

```csharp
var serverSessions = Array.Empty<SessionStatRow>();
var fileIoStats = Array.Empty<IoStatRow>();
var databaseSizes = Array.Empty<DatabaseSizeRow>();
```

Por tanto, no hay que documentarlas como métricas activas en producción si no se habilitan permisos y se descomenta el bloque.

---

## 8. Campos principales requeridos por las consultas

### 8.1. `Transferencia`

Las consultas intra dependen principalmente de:

| Campo | Uso |
|---|---|
| `estado` | Clasificación de estado, errores, pendientes, éxito, compensación. |
| `fechaOperacion` | Ventanas temporales y cálculo de edad. |
| `fechaModificacion` | Latencia y anomalías. |
| `tipoTransferencia` | Agrupación por tipo. |
| `codigoMoneda` | Agrupación de montos por moneda. |
| `monto` | Suma de importes. |

### 8.2. `TransferenciaInterbancaria`

Las consultas inter dependen principalmente de:

| Campo | Uso |
|---|---|
| `estado` | Clasificación de estado, errores, pendientes, éxito, compensación. |
| `fechaOperacion` | Ventanas temporales y cálculo de edad. |
| `fechaModificacion` | Latencia y anomalías. |
| `tipoTransferencia` | Agrupación por tipo. |
| `monedaOperacion` | Agrupación de montos por moneda. |
| `monto` | Suma de importes. |
| `bancoDestino` | Métricas específicas por banco destino. |

---

## 9. Riesgos y observaciones técnicas

### 9.1. Consultas de 7 y 30 días

Las consultas `TxCreated7d`, `TxCreated30d` y `PendingCount7d` pueden ser costosas si las tablas crecen y no hay índices adecuados sobre `fechaOperacion` y `estado`.

El código ya deja una advertencia:

```csharp
// These are more expensive, keeping them for now but they should be moved to offline analysis
```

Recomendación: conservarlas si el volumen es manejable; si el impacto en base aumenta, moverlas a agregados offline, tabla resumen o job de consolidación.

---

### 9.2. Percentiles P95/P99

Las consultas de percentiles usan `PERCENTILE_CONT`, que requiere ordenar duraciones. Esto puede ser pesado sobre grandes ventanas de datos.

Consultas afectadas:

| Consulta |
|---|
| `IntraSuccessSpeed24h` |
| `InterSuccessSpeed24h` |
| `IntraSuccessSpeedP99_24h` |
| `InterSuccessSpeedP99_24h` |

Recomendación: monitorear duración de estas consultas y considerar preagregación si la tabla productiva tiene mucho volumen.

---

### 9.3. Uso de `GETDATE()`

Todas las ventanas temporales se calculan contra la hora del servidor SQL.

Eso es aceptable si el SQL Server está correctamente sincronizado. Si hay diferencias horarias entre servidores, dashboards o contenedores, las ventanas pueden no coincidir exactamente con la percepción operativa.

---

### 9.4. Estados no clasificados

Las consultas:

```text
IntraOtherStateCount24h
InterOtherStateCount24h
```

son importantes porque detectan estados que no entran en la taxonomía actual.

Si empiezan a devolver valores mayores a cero, no se debe ignorar. Puede significar:

- Nuevo estado de negocio.
- Cambio en la aplicación core.
- Error de clasificación.
- Estado histórico no contemplado.

---

### 9.5. Permisos para salud SQL Server

Las consultas de sesiones e I/O requieren permisos adicionales. Si el usuario configurado en `SQLSERVER_CONN` no tiene permisos como `VIEW SERVER STATE`, esas métricas no se deben activar porque podrían fallar.

Actualmente están desactivadas en ejecución, lo cual evita romper el polling.

---

## 10. Recomendaciones de índices

Para reducir impacto en SQL Server, las consultas se beneficiarían de índices enfocados en filtros por fecha y estado.

### 10.1. Para `Transferencia`

```sql
CREATE INDEX IX_Transferencia_FechaOperacion_Estado
ON Transferencia (fechaOperacion, estado)
INCLUDE (fechaModificacion, tipoTransferencia, codigoMoneda, monto);
```

### 10.2. Para `TransferenciaInterbancaria`

```sql
CREATE INDEX IX_TransferenciaInterbancaria_FechaOperacion_Estado
ON TransferenciaInterbancaria (fechaOperacion, estado)
INCLUDE (fechaModificacion, tipoTransferencia, monedaOperacion, monto, bancoDestino);
```

Estos índices son sugeridos, no obligatorios. Antes de aplicarlos en producción se debe revisar el plan de ejecución real, tamaño de tabla, índices existentes y política del DBA.

---

## 11. Recomendación de documentación operativa

Para que el documento no quede ambiguo, conviene manejar estas definiciones en dashboards y alertas:

| Concepto | Debe usar |
|---|---|
| Pendientes operativos reales | Estados `1,6` |
| Revisión crítica | Estado `100` con edad `>= 300s` |
| Programadas | Estados `0,7,17` |
| Rechazadas | Estados `2,4` |
| Fallas técnicas | Estados `5,15` |
| Compensadas | Estado `9` |
| Exitosas/cerradas limpias | Estados `3,8` |
| Estados desconocidos | `OtherStateCount24h` |

No conviene mostrar “pendientes totales” mezclando `0,1,6,7,17,100` como si todos fueran incidentes. Eso puede producir falsos positivos.

---

## 12. Resumen de consultas

### 12.1. Intra

| Grupo | Consultas |
|---|---|
| Estado | `IntraStateCount24h` |
| Pendientes | `IntraOpPendingCount`, `IntraProgrammedCount`, `IntraPendingCount24h`, `IntraPendingCount7d`, `IntraPendingOldestSeconds` |
| Revisión | `IntraReviewStats` |
| Aging | `IntraPendingAgingBucketCount`, `IntraPendingAgeStats` |
| Fallas | `IntraFailures24h`, `IntraErrorCount24h`, `IntraResolvedCount24h`, `IntraCompensatedCurrentCount` |
| Volumen | `IntraTypeCount24h`, `IntraTxCreated5m`, `IntraTxCreated15m`, `IntraTxCreated1h`, `IntraTxCreated24h`, `IntraTxCreated7d`, `IntraTxCreated30d` |
| Montos | `IntraAmountByType24h`, `IntraAmountTotal24h`, `IntraAmountTotal1h` |
| Latencia | `IntraSuccessSpeed24h`, `IntraSuccessSpeedP99_24h`, `IntraResolutionAvgSeconds` |
| Calidad | `IntraZeroDurationCount24h`, `IntraMissingModificationCount24h`, `IntraSuccessCount24h`, `IntraClosedCount24h`, `IntraOtherStateCount24h` |

---

### 12.2. Inter

| Grupo | Consultas |
|---|---|
| Estado | `InterStateCount24h` |
| Pendientes | `InterOpPendingCount`, `InterProgrammedCount`, `InterPendingCount24h`, `InterPendingCount7d`, `InterPendingOldestSeconds` |
| Revisión | `InterReviewStats` |
| Aging | `InterPendingAgingBucketCount`, `InterPendingAgeStats` |
| Fallas | `InterFailures24h`, `InterErrorCount24h`, `InterResolvedCount24h`, `InterCompensatedCurrentCount` |
| Volumen | `InterTypeCount24h`, `InterTxCreated5m`, `InterTxCreated15m`, `InterTxCreated1h`, `InterTxCreated24h`, `InterTxCreated7d`, `InterTxCreated30d` |
| Montos | `InterAmountByType24h`, `InterAmountTotal24h`, `InterAmountTotal1h` |
| Latencia | `InterSuccessSpeed24h`, `InterSuccessSpeedP99_24h`, `InterResolutionAvgSeconds` |
| Banco destino | `InterBankCount24h`, `InterBankStateCount24h`, `InterBankAmountTotal24h` |
| Calidad | `InterZeroDurationCount24h`, `InterMissingModificationCount24h`, `InterSuccessCount24h`, `InterClosedCount24h`, `InterOtherStateCount24h` |

---

### 12.3. Health y calidad general

| Grupo | Consultas |
|---|---|
| Contexto | `GetDayType` |
| SQL Server health | `ServerSessions`, `FileIoStats`, `DatabaseSize` |
| Calidad cruzada | `QualityAnomalies` |

`ServerSessions`, `FileIoStats` y `DatabaseSize` existen, pero actualmente no están activas en la ejecución del polling.

---

## 13. Conclusión

La carpeta `Queries` del proyecto no contiene scripts SQL independientes, sino constantes SQL embebidas en C# dentro de `SqlQueries`.

El diseño actual ya está bastante orientado a observabilidad operativa porque separa:

- Transferencias intra e inter.
- Pendientes reales y programadas.
- Revisión crítica.
- Fallas técnicas y rechazos.
- Montos, volumen y latencia.
- Anomalías de calidad de datos.
- Estados no clasificados.

La parte débil no es la cobertura de consultas, sino el posible impacto de algunas ventanas largas y percentiles si se ejecutan cada minuto contra tablas grandes. Para producción real, las consultas de 7 días, 30 días y percentiles deberían vigilarse de cerca o moverse a agregados si generan carga.
