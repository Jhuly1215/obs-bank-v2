namespace Bank.Obs.SqlPoller.Polling;

public static partial class SqlQueries
{
    // ===================================
    // INTER (TransferenciaInterbancaria)
    // ===================================

    // 1) Distribución por estado 24h
    public const string InterStateCount24h = @"
SELECT estado, COUNT(1) AS [Count] 
FROM TransferenciaInterbancaria 
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE()) 
GROUP BY estado";

    // 2) Backlog activo actual por estado
    public const string InterPendingCurrentCount = $@"
SELECT estado, COUNT(1) AS [Count] 
FROM TransferenciaInterbancaria 
WHERE estado IN ({PendingStates}) AND fechaOperacion IS NOT NULL 
GROUP BY estado";

    // 3) Aging activo por bucket y estado
    public const string InterPendingAgingBucketCount = $@"
SELECT estado, 
       SUM(CASE WHEN DATEDIFF(second, fechaOperacion, GETDATE()) >= 14400 THEN 1 ELSE 0 END) AS Ge14400s,
       SUM(CASE WHEN DATEDIFF(second, fechaOperacion, GETDATE()) >= 3600 THEN 1 ELSE 0 END) AS Ge3600s,
       SUM(CASE WHEN DATEDIFF(second, fechaOperacion, GETDATE()) >= 900 THEN 1 ELSE 0 END) AS Ge900s
FROM TransferenciaInterbancaria 
WHERE estado IN ({PendingStates}) AND fechaOperacion IS NOT NULL 
GROUP BY estado";

    // 4) Edad promedio y máxima del backlog activo por estado
    public const string InterPendingAgeStats = $@"
SELECT estado, 
       ISNULL(AVG(DATEDIFF(second, fechaOperacion, GETDATE())), 0) AS AvgSec, 
       ISNULL(MAX(DATEDIFF(second, fechaOperacion, GETDATE())), 0) AS MaxSec
FROM TransferenciaInterbancaria
WHERE estado IN ({PendingStates}) AND fechaOperacion IS NOT NULL
GROUP BY estado";

    // 5) Rechazos vs fallas técnicas 24h
    public const string InterFailures24h = $@"
SELECT
   SUM(CASE WHEN estado IN ({RejectedStates}) THEN 1 ELSE 0 END) AS Rejected,
   SUM(CASE WHEN estado IN ({FailedTechnicalStates}) THEN 1 ELSE 0 END) AS FailedTechnical
FROM TransferenciaInterbancaria
WHERE estado IN ({RejectedStates}, {FailedTechnicalStates}) AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";

    // 6) Volumen por tipo 24h
    public const string InterTypeCount24h = @"
SELECT ISNULL(CAST(tipoTransferencia AS INT), 0) AS Tipo, COUNT(1) AS [Count] 
FROM TransferenciaInterbancaria 
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE()) 
GROUP BY tipoTransferencia";

    // 7) Monto total 24h por moneda
    public const string InterAmountTotal24h = @"
SELECT ISNULL(CAST(monedaOperacion AS INT), 0) AS Moneda, ISNULL(SUM(monto), 0) AS Total 
FROM TransferenciaInterbancaria 
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE()) 
GROUP BY monedaOperacion";

    // 8) Monto 24h por tipo y moneda
    public const string InterAmountByType24h = @"
SELECT ISNULL(CAST(tipoTransferencia AS INT), 0) AS Tipo, 
       ISNULL(CAST(monedaOperacion AS INT), 0) AS Moneda, 
       ISNULL(SUM(monto), 0) AS Total 
FROM TransferenciaInterbancaria 
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE()) 
GROUP BY tipoTransferencia, monedaOperacion";

    // 9) Duración proxy de éxito por tipo (avg, p95)
    public const string InterSuccessSpeed24h = $@"
SELECT DISTINCT ISNULL(CAST(tipoTransferencia AS INT), 0) AS Tipo,
       ISNULL(AVG(DATEDIFF(second, fechaOperacion, fechaModificacion)) OVER (PARTITION BY tipoTransferencia), 0) AS AvgSec,
       ISNULL(PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY DATEDIFF(second, fechaOperacion, fechaModificacion) ASC) OVER (PARTITION BY tipoTransferencia), 0) AS P95Sec
FROM TransferenciaInterbancaria
WHERE estado IN ({ResolvedSuccessStates}) 
      AND fechaOperacion IS NOT NULL 
      AND fechaModificacion IS NOT NULL
      AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";

    // ===================================
    // METRICAS DE INTERBANCARIAS DESTINO
    // ===================================
    
    // 10.1) Volumen 24h por banco destino
    public const string InterBankCount24h = @"
SELECT ISNULL(CAST(bancoDestino AS INT), 0) AS Banco, COUNT(1) AS [Count]
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE())
GROUP BY bancoDestino";

    // 10.2) Distribución por estado 24h por banco destino
    public const string InterBankStateCount24h = @"
SELECT ISNULL(CAST(bancoDestino AS INT), 0) AS Banco, estado, COUNT(1) AS [Count]
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE())
GROUP BY bancoDestino, estado";

    // 10.3) Monto total 24h por banco destino y moneda
    public const string InterBankAmountTotal24h = @"
SELECT ISNULL(CAST(bancoDestino AS INT), 0) AS Banco, 
       ISNULL(CAST(monedaOperacion AS INT), 0) AS Moneda, 
       ISNULL(SUM(monto), 0) AS Total
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE())
GROUP BY bancoDestino, monedaOperacion";


    // ===================================
    // EXISTENTES / HISTORICAS
    // ===================================
    public const string InterTxCreated15m = "SELECT COUNT(1) FROM TransferenciaInterbancaria WHERE fechaOperacion >= DATEADD(minute, -15, GETDATE())";
    public const string InterTxCreated24h = "SELECT COUNT(1) FROM TransferenciaInterbancaria WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE())";
    public const string InterTxCreated7d  = "SELECT COUNT(1) FROM TransferenciaInterbancaria WHERE fechaOperacion >= DATEADD(day, -7, GETDATE())";
    public const string InterTxCreated30d = "SELECT COUNT(1) FROM TransferenciaInterbancaria WHERE fechaOperacion >= DATEADD(day, -30, GETDATE())";

    public const string InterPendingCount24h = $"SELECT COUNT(1) FROM TransferenciaInterbancaria WHERE estado IN ({PendingStates}) AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";
    public const string InterPendingCount7d  = $"SELECT COUNT(1) FROM TransferenciaInterbancaria WHERE estado IN ({PendingStates}) AND fechaOperacion >= DATEADD(day, -7, GETDATE())";
    public const string InterPendingOldestSeconds = $"SELECT ISNULL(DATEDIFF(second, MIN(fechaOperacion), GETDATE()), 0) FROM TransferenciaInterbancaria WHERE estado IN ({PendingStates})";

    public const string InterErrorCount24h = $"SELECT COUNT(1) FROM TransferenciaInterbancaria WHERE estado IN ({RejectedStates}, {FailedTechnicalStates}) AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";
    
    public const string InterResolvedCount24h = $"SELECT COUNT(1) FROM TransferenciaInterbancaria WHERE estado IN ({ResolvedSuccessStates}) AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";
    public const string InterResolutionAvgSeconds = $"SELECT ISNULL(AVG(DATEDIFF(second, fechaOperacion, ISNULL(fechaModificacion, GETDATE()))), 0) FROM TransferenciaInterbancaria WHERE estado IN ({ResolvedSuccessStates}) AND fechaOperacion >= DATEADD(hour, -24, GETDATE())";
}
