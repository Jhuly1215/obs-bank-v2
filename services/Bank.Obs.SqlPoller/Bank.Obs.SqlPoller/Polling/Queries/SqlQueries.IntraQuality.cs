namespace Bank.Obs.SqlPoller.Polling;

public static partial class SqlQueries
{
    // =========================
    // CALIDAD INTRA
    // Regla: estado 9 = error
    // =========================

    // ---- Conteos 30d ----

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
WHERE estado IN (5,9,15)
  AND fechaOperacion >= DATEADD(day, -30, GETDATE());";

    // ---- Tasas 30d (sobre total en ventana) ----

    public const string IntraSuccessRate30d = @"
SELECT CAST(1.0 * SUM(CASE WHEN estado = 3 THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS FLOAT)
FROM Transferencia
WHERE fechaOperacion >= DATEADD(day, -30, GETDATE());";

    public const string IntraRejectRate30d = @"
SELECT CAST(1.0 * SUM(CASE WHEN estado IN (2,4) THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS FLOAT)
FROM Transferencia
WHERE fechaOperacion >= DATEADD(day, -30, GETDATE());";

    public const string IntraFailTechRate30d = @"
SELECT CAST(1.0 * SUM(CASE WHEN estado IN (5,9,15) THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS FLOAT)
FROM Transferencia
WHERE fechaOperacion >= DATEADD(day, -30, GETDATE());";

    // ---- Conteos 24h ----

    public const string IntraSuccessCount24h = @"
SELECT COUNT(*)
FROM Transferencia
WHERE estado = 3
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE());";

    public const string IntraRejectCount24h = @"
SELECT COUNT(*)
FROM Transferencia
WHERE estado IN (2,4)
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE());";

    public const string IntraFailTechCount24h = @"
SELECT COUNT(*)
FROM Transferencia
WHERE estado IN (5,9,15)
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE());";

    // ---- Tasas 24h (sobre total en ventana) ----

    public const string IntraSuccessRate24h = @"
SELECT CAST(1.0 * SUM(CASE WHEN estado = 3 THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS FLOAT)
FROM Transferencia
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE());";

    public const string IntraRejectRate24h = @"
SELECT CAST(1.0 * SUM(CASE WHEN estado IN (2,4) THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS FLOAT)
FROM Transferencia
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE());";

    public const string IntraFailTechRate24h = @"
SELECT CAST(1.0 * SUM(CASE WHEN estado IN (5,9,15) THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS FLOAT)
FROM Transferencia
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE());";
}