namespace Bank.Obs.SqlPoller.Polling;

public static partial class SqlQueries
{
    // ===================================
    // INTER (TransferenciaInterbancaria)
    // ===================================

    // ===================================
    // INTER (TransferenciaInterbancaria)
    // ===================================

    // 1) Distribución por estado 24h
    public const string InterStateCount24h = @"
SELECT estado, COUNT(1) AS [Count] 
FROM TransferenciaInterbancaria 
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE()) 
GROUP BY estado";

    // 2) Backlog Operativo Real (1, 6)
    public const string InterOpPendingCount = $@"
SELECT estado, COUNT(1) AS [Count] 
FROM TransferenciaInterbancaria 
WHERE estado IN ({OpPendingStates}) AND fechaOperacion IS NOT NULL 
GROUP BY estado";

    // 3) Backlog Programado (0, 7, 17)
    public const string InterProgrammedCount = $@"
SELECT estado, COUNT(1) AS [Count] 
FROM TransferenciaInterbancaria 
WHERE estado IN ({ProgrammedStates}) AND fechaOperacion IS NOT NULL 
GROUP BY estado";

    // 4) Estado 100 (Review) - Métricas Críticas ACH
    public const string InterReviewStats = $@"
SELECT 
    COUNT(1) AS TotalCount,
    ISNULL(AVG(DATEDIFF(second, fechaOperacion, GETDATE())), 0) AS AvgSec,
    ISNULL(MAX(DATEDIFF(second, fechaOperacion, GETDATE())), 0) AS MaxSec,
    ISNULL(SUM(CASE WHEN DATEDIFF(second, fechaOperacion, GETDATE()) >= 300 THEN 1 ELSE 0 END), 0) AS DeadCount
FROM TransferenciaInterbancaria
WHERE estado IN ({ReviewStates}) AND fechaOperacion IS NOT NULL";

    // 5) Aging granular (Solo Operativas + 100)
    public const string InterPendingAgingBucketCount = $@"
SELECT estado, 
        ISNULL(SUM(CASE WHEN DATEDIFF(second, fechaOperacion, GETDATE()) >= 14400 THEN 1 ELSE 0 END), 0) AS Ge14400s,
       ISNULL(SUM(CASE WHEN DATEDIFF(second, fechaOperacion, GETDATE()) >= 3600 THEN 1 ELSE 0 END), 0) AS Ge3600s,
       ISNULL(SUM(CASE WHEN DATEDIFF(second, fechaOperacion, GETDATE()) >= 900 THEN 1 ELSE 0 END), 0) AS Ge900s
FROM TransferenciaInterbancaria 
WHERE estado IN ({OpPendingStates}, {ReviewStates}) AND fechaOperacion IS NOT NULL 
GROUP BY estado";

    // 6) Fallas, Rechazos y Compensadas 24h
    public const string InterFailures24h = $@"
SELECT
   ISNULL(SUM(CASE WHEN estado IN ({RejectedStates}) THEN 1 ELSE 0 END), 0) AS Rejected,
   ISNULL(SUM(CASE WHEN estado IN ({TechFailedStates}) THEN 1 ELSE 0 END), 0) AS FailedTechnical,
   ISNULL(SUM(CASE WHEN estado IN ({CompensatedStates}) THEN 1 ELSE 0 END), 0) AS Compensated
FROM TransferenciaInterbancaria
WHERE estado IN ({RejectedStates}, {TechFailedStates}, {CompensatedStates}) 
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";

    // 7) Volumen por tipo 24h
    public const string InterTypeCount24h = @"
SELECT ISNULL(CAST(tipoTransferencia AS INT), 0) AS Tipo, COUNT(1) AS [Count] 
FROM TransferenciaInterbancaria 
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE()) 
GROUP BY tipoTransferencia";

    // 8) Monto 24h por tipo y moneda
    public const string InterAmountByType24h = @"
SELECT ISNULL(CAST(tipoTransferencia AS INT), 0) AS Tipo, 
       ISNULL(CAST(monedaOperacion AS INT), 0) AS Moneda, 
       ISNULL(SUM(monto), 0) AS Total 
FROM TransferenciaInterbancaria 
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE()) 
GROUP BY tipoTransferencia, monedaOperacion";

    // 9) Optimización de Latencia (P95/Avg)
    public const string InterSuccessSpeed24h = $@"
WITH SuccessBase AS (
    SELECT 
        ISNULL(CAST(tipoTransferencia AS INT), 0) AS Tipo,
        DATEDIFF(second, fechaOperacion, fechaModificacion) AS dur_s
    FROM TransferenciaInterbancaria
    WHERE estado IN ({SuccessStates}) 
      AND fechaOperacion IS NOT NULL 
      AND fechaModificacion IS NOT NULL
      AND fechaOperacion >= DATEADD(hour, -24, GETDATE())
)
SELECT DISTINCT Tipo,
       AVG(dur_s) OVER (PARTITION BY Tipo) AS AvgSec,
       PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY dur_s ASC) OVER (PARTITION BY Tipo) AS P95Sec
FROM SuccessBase";

    // 10) Métricas por Banco Destino (Optimizadas)
    public const string InterBankCount24h = @"
SELECT ISNULL(CAST(bancoDestino AS INT), 0) AS Banco, COUNT(1) AS [Count]
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE())
GROUP BY bancoDestino";

    public const string InterBankStateCount24h = @"
SELECT ISNULL(CAST(bancoDestino AS INT), 0) AS Banco, estado, COUNT(1) AS [Count]
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE())
GROUP BY bancoDestino, estado";

    public const string InterBankAmountTotal24h = @"
SELECT ISNULL(CAST(bancoDestino AS INT), 0) AS Banco, 
       ISNULL(CAST(monedaOperacion AS INT), 0) AS Moneda, 
       ISNULL(SUM(monto), 0) AS Total
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE())
GROUP BY bancoDestino, monedaOperacion";

    // Tactical Windows
    public const string InterTxCreated5m = "SELECT COUNT(1) FROM TransferenciaInterbancaria WHERE fechaOperacion >= DATEADD(minute, -5, GETDATE())";
    public const string InterTxCreated15m = "SELECT COUNT(1) FROM TransferenciaInterbancaria WHERE fechaOperacion >= DATEADD(minute, -15, GETDATE())";
    public const string InterTxCreated1h = "SELECT COUNT(1) FROM TransferenciaInterbancaria WHERE fechaOperacion >= DATEADD(hour, -1, GETDATE())";
    public const string InterTxCreated24h = "SELECT COUNT(1) FROM TransferenciaInterbancaria WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE())";
    public const string InterTxCreated7d = "SELECT COUNT(1) FROM TransferenciaInterbancaria WHERE fechaOperacion >= DATEADD(day, -7, GETDATE())";
    public const string InterTxCreated30d = "SELECT COUNT(1) FROM TransferenciaInterbancaria WHERE fechaOperacion >= DATEADD(day, -30, GETDATE())";

    public const string InterPendingCount24h = $@"SELECT COUNT(1) FROM TransferenciaInterbancaria WHERE estado IN ({OpPendingStates}, {ReviewStates}) AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";
    public const string InterPendingCount7d = $@"SELECT COUNT(1) FROM TransferenciaInterbancaria WHERE estado IN ({OpPendingStates}, {ReviewStates}) AND fechaOperacion >= DATEADD(day, -7, GETDATE())";

    public const string InterErrorCount24h = $@"SELECT COUNT(1) FROM TransferenciaInterbancaria WHERE estado IN ({RejectedStates}, {TechFailedStates}) AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";
    public const string InterResolvedCount24h = $@"SELECT COUNT(1) FROM TransferenciaInterbancaria WHERE estado IN ({SuccessStates}, {CompensatedStates}) AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";

    // Legacy Support (Oldest excluding programmed)
    public const string InterPendingOldestSeconds = $@"
SELECT ISNULL(DATEDIFF(second, MIN(fechaOperacion), GETDATE()), 0) 
FROM TransferenciaInterbancaria 
WHERE estado IN ({OpPendingStates}, {ReviewStates})";

    public const string InterResolutionAvgSeconds = $@"
SELECT ISNULL(AVG(DATEDIFF(second, fechaOperacion, fechaModificacion)), 0) 
FROM TransferenciaInterbancaria 
WHERE estado IN ({SuccessStates}) 
  AND fechaModificacion IS NOT NULL
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";

    // P99 Standalone
    public const string InterSuccessSpeedP99_24h = $@"
WITH SuccessBase AS (
    SELECT DATEDIFF(second, fechaOperacion, fechaModificacion) AS dur_s
    FROM TransferenciaInterbancaria
    WHERE estado IN ({SuccessStates}) 
      AND fechaOperacion IS NOT NULL 
      AND fechaModificacion IS NOT NULL
      AND fechaOperacion >= DATEADD(hour, -24, GETDATE())
)
SELECT TOP 1 ISNULL(PERCENTILE_CONT(0.99) WITHIN GROUP (ORDER BY dur_s ASC) OVER(), 0) FROM SuccessBase";

    // Anomalies
    public const string InterZeroDurationCount24h = $@"
SELECT COUNT(1) FROM TransferenciaInterbancaria 
WHERE estado IN ({SuccessStates}) 
  AND DATEDIFF(second, fechaOperacion, fechaModificacion) = 0
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";

    public const string InterMissingModificationCount24h = $@"
SELECT COUNT(1) FROM TransferenciaInterbancaria 
WHERE estado IN ({SuccessStates}) 
  AND fechaModificacion IS NULL
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";
}
