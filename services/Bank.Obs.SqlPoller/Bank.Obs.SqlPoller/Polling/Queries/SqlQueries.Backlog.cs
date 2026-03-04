namespace Bank.Obs.SqlPoller.Polling;

public static partial class SqlQueries
{
    // =========================
    // BACKLOG / ATASCO (PROXY)
    // Regla: estado 9 = error (NO pendiente)
    // =========================

    // ---- Pendientes (snapshot por estado actual) - 7d ----

    public const string IntraPendingLast7d = @"
SELECT COUNT(*)
FROM Transferencia
WHERE estado IN (0,1,6,7,8,100)
  AND fechaOperacion >= DATEADD(day, -7, GETDATE());";

    public const string InterPendingLast7d = @"
SELECT COUNT(*)
FROM TransferenciaInterbancaria
WHERE estado IN (1,6)
  AND fechaOperacion >= DATEADD(day, -7, GETDATE());";

    // ---- Edad máxima de pendientes (minutos) - 7d ----

    public const string IntraPendingMaxAgeMin = @"
SELECT CAST(ISNULL(MAX(DATEDIFF(MINUTE, fechaOperacion, GETDATE())),0) AS FLOAT)
FROM Transferencia
WHERE estado IN (0,1,6,7,8,100)
  AND fechaOperacion >= DATEADD(day, -7, GETDATE());";

    public const string InterPendingMaxAgeMin = @"
SELECT CAST(ISNULL(MAX(DATEDIFF(MINUTE, fechaOperacion, GETDATE())),0) AS FLOAT)
FROM TransferenciaInterbancaria
WHERE estado IN (1,6)
  AND fechaOperacion >= DATEADD(day, -7, GETDATE());";

    // ---- Pendientes creados en 24h (carga hacia el backlog) ----

    public const string IntraPendingCount24h = @"
SELECT COUNT(*)
FROM Transferencia
WHERE estado IN (0,1,6,7,8,100)
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE());";

    public const string InterPendingCount24h = @"
SELECT COUNT(*)
FROM TransferenciaInterbancaria
WHERE estado IN (1,6)
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE());";

    // ---- Pendientes por estado (tabular) - 7d ----
    // OJO: multi-row. Si tu poller solo soporta scalar, úsalo en endpoint/tabla, no como métrica scalar.

    public const string IntraPendingByState7d = @"
SELECT estado, COUNT(*) AS cnt
FROM Transferencia
WHERE estado IN (0,1,6,7,8,100)
  AND fechaOperacion >= DATEADD(day, -7, GETDATE())
GROUP BY estado
ORDER BY cnt DESC;";

    public const string InterPendingByState7d = @"
SELECT estado, COUNT(*) AS cnt
FROM TransferenciaInterbancaria
WHERE estado IN (1,6)
  AND fechaOperacion >= DATEADD(day, -7, GETDATE())
GROUP BY estado
ORDER BY cnt DESC;";
}