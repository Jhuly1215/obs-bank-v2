namespace Bank.Obs.SqlPoller.Polling;

public static class SqlQueries
{
    // =========================
    // VOLUMEN / ACTIVIDAD (registro observado)
    // =========================

    public const string IntraTxLast30d = @"
SELECT COUNT(*) 
FROM Transferencia
WHERE fechaOperacion >= DATEADD(day, -30, GETDATE());";

    public const string InterTxLast30d = @"
SELECT COUNT(*)
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(day, -30, GETDATE());";

    public const string IntraTxLast15m = @"
SELECT COUNT(*)
FROM Transferencia
WHERE fechaOperacion >= DATEADD(minute, -15, GETDATE());";

    public const string InterTxLast15m = @"
SELECT COUNT(*)
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(minute, -15, GETDATE());";

    // =========================
    // CALIDAD INTRA
    // =========================

    public const string IntraSuccessCount30d = @"
SELECT COUNT(*)
FROM Transferencia
WHERE estado = 3
  AND fechaOperacion >= DATEADD(day, -30, GETDATE());";

    public const string IntraRejectCount30d = @"
SELECT COUNT(*)
FROM Transferencia
WHERE estado IN (2,4)
  AND fechaOperacion >= DATEADD(day, -30, GETDATE());";

    public const string IntraFailTechCount30d = @"
SELECT COUNT(*)
FROM Transferencia
WHERE estado IN (5,15)
  AND fechaOperacion >= DATEADD(day, -30, GETDATE());";

    public const string IntraSuccessRate30d = @"
SELECT CAST(1.0 * SUM(CASE WHEN estado = 3 THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS FLOAT)
FROM Transferencia
WHERE fechaOperacion >= DATEADD(day, -30, GETDATE());";

    public const string IntraRejectRate30d = @"
SELECT CAST(1.0 * SUM(CASE WHEN estado IN (2,4) THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS FLOAT)
FROM Transferencia
WHERE fechaOperacion >= DATEADD(day, -30, GETDATE());";

    public const string IntraFailTechRate30d = @"
SELECT CAST(1.0 * SUM(CASE WHEN estado IN (5,15) THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS FLOAT)
FROM Transferencia
WHERE fechaOperacion >= DATEADD(day, -30, GETDATE());";

    // =========================
    // BACKLOG / ATASCO (PROXY)
    // =========================

    public const string IntraPendingLast7d = @"
SELECT COUNT(*)
FROM Transferencia
WHERE estado IN (0,1,6,7,8,9,100)
  AND fechaOperacion >= DATEADD(day, -7, GETDATE());";

    public const string InterPendingProxyLast7d = @"
SELECT COUNT(*)
FROM TransferenciaInterbancaria
WHERE estado IN (1,6,9)
  AND fechaOperacion >= DATEADD(day, -7, GETDATE());";

    public const string IntraPendingMaxAgeMin = @"
SELECT CAST(ISNULL(MAX(DATEDIFF(MINUTE, fechaOperacion, GETDATE())),0) AS FLOAT)
FROM Transferencia
WHERE estado IN (0,1,6,7,8,9,100)
  AND fechaOperacion >= DATEADD(day, -7, GETDATE());";

    public const string InterPendingMaxAgeMin = @"
SELECT CAST(ISNULL(MAX(DATEDIFF(MINUTE, fechaOperacion, GETDATE())),0) AS FLOAT)
FROM TransferenciaInterbancaria
WHERE estado IN (1,6,9)
  AND fechaOperacion >= DATEADD(day, -7, GETDATE());";

    // =========================
    // INTERBANCARIO (semántica parcial)
    // =========================

    public const string InterState9ObservedShare30d = @"
SELECT CAST(1.0 * SUM(CASE WHEN estado = 9 THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS FLOAT)
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(day, -30, GETDATE());";

    public const string InterFailTechRate30d = @"
SELECT CAST(1.0 * SUM(CASE WHEN estado = 5 THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS FLOAT)
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(day, -30, GETDATE());";

    public const string InterSuccessRate30d = @"
SELECT CAST(1.0 * SUM(CASE WHEN estado = 3 THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS FLOAT)
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(day, -30, GETDATE());";

    public const string InterUnknownStateCount30d = @"
SELECT COUNT(*)
FROM TransferenciaInterbancaria
WHERE estado NOT IN (1,3,5,6,9)
  AND fechaOperacion >= DATEADD(day, -30, GETDATE());";

    public const string InterPendingLast7d = InterPendingProxyLast7d;
    public const string IntraState9InterShare30d = InterState9ObservedShare30d;
}