namespace Bank.Obs.SqlPoller.Polling;

public static partial class SqlQueries
{
    // ===================================
    // NEW TAXONOMY (Refactored)
    // ===================================
    public const string ProgrammedStates = "0,7,17";
    public const string OpPendingStates = "1,6";
    public const string ReviewStates = "100";
    public const string RejectedStates = "2,4";
    public const string TechFailedStates = "5,15";
    public const string CompensatedStates = "9";
    public const string SuccessStates = "3,8"; // Validated: 3 is success, 8 is cleanly reverted/liquidated

    // Combined for legacy/all pending dashboards
    public const string AllPendingStates = "0,1,6,7,17,100";

    // ===================================
    // INTRA (Transferencia)
    // ===================================

    // 1) Distribución por estado 24h
    public const string IntraStateCount24h = @"
SELECT estado, COUNT(1) AS [Count] 
FROM Transferencia 
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE()) 
GROUP BY estado";

    // 2) Backlog Operativo Real (1, 6)
    public const string IntraOpPendingCount = $@"
SELECT estado, COUNT(1) AS [Count] 
FROM Transferencia 
WHERE estado IN ({OpPendingStates}) AND fechaOperacion IS NOT NULL 
GROUP BY estado";

    // 3) Backlog Programado (0, 7, 17)
    public const string IntraProgrammedCount = $@"
SELECT estado, COUNT(1) AS [Count] 
FROM Transferencia 
WHERE estado IN ({ProgrammedStates}) AND fechaOperacion IS NOT NULL 
GROUP BY estado";

    // 4) Estado 100 (Review) - Métricas Críticas
    public const string IntraReviewStats = $@"
SELECT 
    COUNT(1) AS TotalCount,
    ISNULL(AVG(DATEDIFF(second, fechaOperacion, GETDATE())), 0) AS AvgSec,
    ISNULL(MAX(DATEDIFF(second, fechaOperacion, GETDATE())), 0) AS MaxSec,
    ISNULL(SUM(CASE WHEN DATEDIFF(second, fechaOperacion, GETDATE()) >= 300 THEN 1 ELSE 0 END), 0) AS DeadCount
FROM Transferencia
WHERE estado IN ({ReviewStates}) AND fechaOperacion IS NOT NULL";

    // 5) Aging granular (Solo Operativas + 100, excluye programadas)
    public const string IntraPendingAgingBucketCount = $@"
SELECT estado, 
        ISNULL(SUM(CASE WHEN DATEDIFF(second, fechaOperacion, GETDATE()) >= 14400 THEN 1 ELSE 0 END), 0) AS Ge14400s,
       ISNULL(SUM(CASE WHEN DATEDIFF(second, fechaOperacion, GETDATE()) >= 3600 THEN 1 ELSE 0 END), 0) AS Ge3600s,
       ISNULL(SUM(CASE WHEN DATEDIFF(second, fechaOperacion, GETDATE()) >= 900 THEN 1 ELSE 0 END), 0) AS Ge900s
FROM Transferencia 
WHERE estado IN ({OpPendingStates}, {ReviewStates}) AND fechaOperacion IS NOT NULL 
GROUP BY estado";

    // 6) Fallas, Rechazos y Compensadas 24h (Estado 9 corregido)
    public const string IntraFailures24h = $@"
SELECT
   ISNULL(SUM(CASE WHEN estado IN ({RejectedStates}) THEN 1 ELSE 0 END), 0) AS Rejected,
   ISNULL(SUM(CASE WHEN estado IN ({TechFailedStates}) THEN 1 ELSE 0 END), 0) AS FailedTechnical,
   ISNULL(SUM(CASE WHEN estado IN ({CompensatedStates}) THEN 1 ELSE 0 END), 0) AS Compensated
FROM Transferencia
WHERE estado IN ({RejectedStates}, {TechFailedStates}, {CompensatedStates}) 
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";

    // 7) Volumen por tipo 24h
    public const string IntraTypeCount24h = @"
SELECT ISNULL(CAST(tipoTransferencia AS INT), 0) AS Tipo, COUNT(1) AS [Count] 
FROM Transferencia 
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE()) 
GROUP BY tipoTransferencia";

    // 8) Monto 24h por tipo y moneda
    public const string IntraAmountByType24h = @"
SELECT ISNULL(CAST(tipoTransferencia AS INT), 0) AS Tipo, 
       ISNULL(CAST(codigoMoneda AS INT), 0) AS Moneda, 
       ISNULL(SUM(monto), 0) AS Total 
FROM Transferencia 
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE()) 
GROUP BY tipoTransferencia, codigoMoneda";

    // 9) Optimización de Latencia (P95/Avg) - Solo Success
    public const string IntraSuccessSpeed24h = $@"
WITH SuccessBase AS (
    SELECT 
        ISNULL(CAST(tipoTransferencia AS INT), 0) AS Tipo,
        DATEDIFF(second, fechaOperacion, fechaModificacion) AS dur_s
    FROM Transferencia
    WHERE estado IN ({SuccessStates}) 
      AND fechaOperacion IS NOT NULL 
      AND fechaModificacion IS NOT NULL
      AND fechaOperacion >= DATEADD(hour, -24, GETDATE())
)
SELECT DISTINCT Tipo,
       AVG(dur_s) OVER (PARTITION BY Tipo) AS AvgSec,
       PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY dur_s ASC) OVER (PARTITION BY Tipo) AS P95Sec
FROM SuccessBase";

    // 10) P99 Standalone (Optimizado)
    public const string IntraSuccessSpeedP99_24h = $@"
WITH SuccessBase AS (
    SELECT DATEDIFF(second, fechaOperacion, fechaModificacion) AS dur_s
    FROM Transferencia
    WHERE estado IN ({SuccessStates}) 
      AND fechaOperacion IS NOT NULL 
      AND fechaModificacion IS NOT NULL
      AND fechaOperacion >= DATEADD(hour, -24, GETDATE())
)
SELECT TOP 1 ISNULL(PERCENTILE_CONT(0.99) WITHIN GROUP (ORDER BY dur_s ASC) OVER(), 0) FROM SuccessBase";

    // Historical Volume (Tactical windows)
    public const string IntraTxCreated5m = "SELECT COUNT(1) FROM Transferencia WHERE fechaOperacion >= DATEADD(minute, -5, GETDATE())";
    public const string IntraTxCreated15m = "SELECT COUNT(1) FROM Transferencia WHERE fechaOperacion >= DATEADD(minute, -15, GETDATE())";
    public const string IntraTxCreated1h = "SELECT COUNT(1) FROM Transferencia WHERE fechaOperacion >= DATEADD(hour, -1, GETDATE())";
    public const string IntraTxCreated24h = "SELECT COUNT(1) FROM Transferencia WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE())";

    public const string IntraTxCreated7d = "SELECT COUNT(1) FROM Transferencia WHERE fechaOperacion >= DATEADD(day, -7, GETDATE())";
    public const string IntraTxCreated30d = "SELECT COUNT(1) FROM Transferencia WHERE fechaOperacion >= DATEADD(day, -30, GETDATE())";

    public const string IntraPendingCount24h = $@"SELECT COUNT(1) FROM Transferencia WHERE estado IN ({OpPendingStates}, {ReviewStates}) AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";
    public const string IntraPendingCount7d = $@"SELECT COUNT(1) FROM Transferencia WHERE estado IN ({OpPendingStates}, {ReviewStates}) AND fechaOperacion >= DATEADD(day, -7, GETDATE())";

    public const string IntraErrorCount24h = $@"SELECT COUNT(1) FROM Transferencia WHERE estado IN ({RejectedStates}, {TechFailedStates}) AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";
    public const string IntraResolvedCount24h = $@"SELECT COUNT(1) FROM Transferencia WHERE estado IN ({SuccessStates}, {CompensatedStates}) AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";

    public const string IntraPendingOldestSeconds = $@"
SELECT ISNULL(DATEDIFF(second, MIN(fechaOperacion), GETDATE()), 0) 
FROM Transferencia 
WHERE estado IN ({OpPendingStates}, {ReviewStates})";

    public const string IntraResolutionAvgSeconds = $@"
SELECT ISNULL(AVG(DATEDIFF(second, fechaOperacion, fechaModificacion)), 0) 
FROM Transferencia 
WHERE estado IN ({SuccessStates}) 
  AND fechaModificacion IS NOT NULL
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";

    // Anomalies
    public const string IntraZeroDurationCount24h = $@"
SELECT COUNT(1) FROM Transferencia 
WHERE estado IN ({SuccessStates}) 
  AND DATEDIFF(second, fechaOperacion, fechaModificacion) = 0
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";

    public const string IntraMissingModificationCount24h = $@"
SELECT COUNT(1) FROM Transferencia 
WHERE estado IN ({SuccessStates}) 
  AND fechaModificacion IS NULL
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";

    public const string IntraSuccessCount24h = $@"
SELECT COUNT(1)
FROM Transferencia
WHERE estado IN ({SuccessStates})
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";

    public const string IntraPendingAgeStats = $@"
SELECT
    estado,
    ISNULL(AVG(DATEDIFF(second, fechaOperacion, GETDATE())), 0) AS AvgSec,
    ISNULL(MAX(DATEDIFF(second, fechaOperacion, GETDATE())), 0) AS MaxSec
FROM Transferencia
WHERE estado IN ({OpPendingStates}, {ReviewStates})
  AND fechaOperacion IS NOT NULL
GROUP BY estado";

    public const string IntraAmountTotal24h = @"
SELECT
    ISNULL(CAST(codigoMoneda AS INT), 0) AS Moneda,
    ISNULL(SUM(monto), 0) AS Total
FROM Transferencia
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE())
GROUP BY codigoMoneda";

    public const string IntraAmountTotal1h = @"
SELECT ISNULL(SUM(monto), 0)
FROM Transferencia
WHERE fechaOperacion >= DATEADD(hour, -1, GETDATE())";

    public const string IntraClosedCount24h = $@"
SELECT COUNT(1)
FROM Transferencia
WHERE estado IN ({SuccessStates}, {CompensatedStates}, {RejectedStates}, {TechFailedStates})
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";

    public const string IntraOtherStateCount24h = $@"
SELECT COUNT(1)
FROM Transferencia
WHERE estado NOT IN ({ProgrammedStates}, {OpPendingStates}, {ReviewStates}, {RejectedStates}, {TechFailedStates}, {CompensatedStates}, {SuccessStates})
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";

    public const string IntraCompensatedCurrentCount = $@"
SELECT COUNT(1)
FROM Transferencia
WHERE estado IN ({CompensatedStates})
  AND fechaOperacion IS NOT NULL";

}
