namespace Bank.Obs.SqlPoller.Polling;

public static partial class SqlQueries
{
    // ===================================
    // COMMON STATES
    // ===================================
    public const string PendingStates = "0,1,6,7,17,100";
    public const string RejectedStates = "2,4";
    public const string FailedTechnicalStates = "5,9,15";
    public const string ResolvedSuccessStates = "3,8"; // 8 is cleanly reverted

    // ===================================
    // INTRA (Transferencia)
    // ===================================

    // 1) Distribución por estado 24h
    public const string IntraStateCount24h = @"
SELECT estado, COUNT(1) AS [Count] 
FROM Transferencia 
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE()) 
GROUP BY estado";

    // 2) Backlog activo actual por estado
    public const string IntraPendingCurrentCount = $@"
SELECT estado, COUNT(1) AS [Count] 
FROM Transferencia 
WHERE estado IN ({PendingStates}) AND fechaOperacion IS NOT NULL 
GROUP BY estado";

    // 3) Aging activo por bucket y estado
    public const string IntraPendingAgingBucketCount = $@"
SELECT estado, 
       SUM(CASE WHEN DATEDIFF(second, fechaOperacion, GETDATE()) >= 14400 THEN 1 ELSE 0 END) AS Ge14400s,
       SUM(CASE WHEN DATEDIFF(second, fechaOperacion, GETDATE()) >= 3600 THEN 1 ELSE 0 END) AS Ge3600s,
       SUM(CASE WHEN DATEDIFF(second, fechaOperacion, GETDATE()) >= 900 THEN 1 ELSE 0 END) AS Ge900s
FROM Transferencia 
WHERE estado IN ({PendingStates}) AND fechaOperacion IS NOT NULL 
GROUP BY estado";

    // 4) Edad promedio y máxima del backlog activo por estado
    public const string IntraPendingAgeStats = $@"
SELECT estado, 
       ISNULL(AVG(DATEDIFF(second, fechaOperacion, GETDATE())), 0) AS AvgSec, 
       ISNULL(MAX(DATEDIFF(second, fechaOperacion, GETDATE())), 0) AS MaxSec
FROM Transferencia
WHERE estado IN ({PendingStates}) AND fechaOperacion IS NOT NULL
GROUP BY estado";

    // 5) Rechazos vs fallas técnicas 24h
    // One row returned with two columns
    public const string IntraFailures24h = $@"
SELECT
   SUM(CASE WHEN estado IN ({RejectedStates}) THEN 1 ELSE 0 END) AS Rejected,
   SUM(CASE WHEN estado IN ({FailedTechnicalStates}) THEN 1 ELSE 0 END) AS FailedTechnical
FROM Transferencia
WHERE estado IN ({RejectedStates}, {FailedTechnicalStates}) AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";

    // 6) Volumen por tipo 24h
    public const string IntraTypeCount24h = @"
SELECT ISNULL(CAST(tipoTransferencia AS INT), 0) AS Tipo, COUNT(1) AS [Count] 
FROM Transferencia 
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE()) 
GROUP BY tipoTransferencia";

    // 7) Monto total 24h por moneda
    public const string IntraAmountTotal24h = @"
SELECT ISNULL(CAST(codigoMoneda AS INT), 0) AS Moneda, ISNULL(SUM(monto), 0) AS Total 
FROM Transferencia 
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE()) 
GROUP BY codigoMoneda";

    // 8) Monto 24h por tipo y moneda
    public const string IntraAmountByType24h = @"
SELECT ISNULL(CAST(tipoTransferencia AS INT), 0) AS Tipo, 
       ISNULL(CAST(codigoMoneda AS INT), 0) AS Moneda, 
       ISNULL(SUM(monto), 0) AS Total 
FROM Transferencia 
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE()) 
GROUP BY tipoTransferencia, codigoMoneda";

    // 9) Duración proxy de éxito por tipo
    public const string IntraSuccessSpeed24h = $@"
SELECT DISTINCT ISNULL(CAST(tipoTransferencia AS INT), 0) AS Tipo,
       ISNULL(AVG(DATEDIFF(second, fechaOperacion, fechaModificacion)) OVER (PARTITION BY tipoTransferencia), 0) AS AvgSec,
       ISNULL(PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY DATEDIFF(second, fechaOperacion, fechaModificacion) ASC) OVER (PARTITION BY tipoTransferencia), 0) AS P95Sec
FROM Transferencia
WHERE estado IN ({ResolvedSuccessStates}) 
      AND fechaOperacion IS NOT NULL 
      AND fechaModificacion IS NOT NULL
      AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";

    // Historical Volume queries (Keeping the simplest ones for general 15m/etc tracking)
    public const string IntraTxCreated15m = "SELECT COUNT(1) FROM Transferencia WHERE fechaOperacion >= DATEADD(minute, -15, GETDATE())";
    public const string IntraTxCreated24h = "SELECT COUNT(1) FROM Transferencia WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE())";
    public const string IntraTxCreated7d  = "SELECT COUNT(1) FROM Transferencia WHERE fechaOperacion >= DATEADD(day, -7, GETDATE())";
    public const string IntraTxCreated30d = "SELECT COUNT(1) FROM Transferencia WHERE fechaOperacion >= DATEADD(day, -30, GETDATE())";

    // Original pending count/oldest to prevent breaking existing dashboard
    public const string IntraPendingCount24h = $"SELECT COUNT(1) FROM Transferencia WHERE estado IN ({PendingStates}) AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";
    public const string IntraPendingCount7d  = $"SELECT COUNT(1) FROM Transferencia WHERE estado IN ({PendingStates}) AND fechaOperacion >= DATEADD(day, -7, GETDATE())";
    public const string IntraPendingOldestSeconds = $"SELECT ISNULL(DATEDIFF(second, MIN(fechaOperacion), GETDATE()), 0) FROM Transferencia WHERE estado IN ({PendingStates})";

    public const string IntraErrorCount24h = $"SELECT COUNT(1) FROM Transferencia WHERE estado IN ({RejectedStates}, {FailedTechnicalStates}) AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";
    
    // For original res queries 
    public const string IntraResolvedCount24h = $"SELECT COUNT(1) FROM Transferencia WHERE estado IN ({ResolvedSuccessStates}) AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";
    public const string IntraResolutionAvgSeconds = $"SELECT ISNULL(AVG(DATEDIFF(second, fechaOperacion, ISNULL(fechaModificacion, GETDATE()))), 0) FROM Transferencia WHERE estado IN ({ResolvedSuccessStates}) AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";
}
