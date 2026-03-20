namespace Bank.Obs.SqlPoller.Polling;

public static partial class SqlQueries
{
    // =========================
    // ERRORES (24h / 7d) + DESGLOSE POR ESTADO
    // Estado 9 se trata como error
    // =========================

    // -------- INTRA: conteos de error 24h / 7d --------

    public const string IntraErrorCount24h = @"
SELECT COUNT(*)
FROM Transferencia
WHERE estado IN (5,9,15)
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE());";

    public const string IntraErrorCount7d = @"
SELECT COUNT(*)
FROM Transferencia
WHERE estado IN (5,9,15)
  AND fechaOperacion >= DATEADD(day, -7, GETDATE());";

    // -------- INTER: conteos de error 24h / 7d --------

    public const string InterErrorCount24h = @"
SELECT COUNT(*)
FROM TransferenciaInterbancaria
WHERE estado IN (5,9)
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE());";

    public const string InterErrorCount7d = @"
SELECT COUNT(*)
FROM TransferenciaInterbancaria
WHERE estado IN (5,9)
  AND fechaOperacion >= DATEADD(day, -7, GETDATE());";

    // -------- ENTRADAS A ERROR POR ESTADO (proxy) --------
    // Usa fechaModificacion como aproximación de "entraron a estado X" en las últimas 24h.

    public const string IntraErrorsEnteredByState24h = @"
SELECT estado, COUNT(*) AS cnt
FROM Transferencia
WHERE estado IN (5,9,15)
  AND fechaModificacion >= DATEADD(hour, -24, GETDATE())
GROUP BY estado
ORDER BY cnt DESC;";

    public const string InterErrorsEnteredByState24h = @"
SELECT estado, COUNT(*) AS cnt
FROM TransferenciaInterbancaria
WHERE estado IN (5,9)
  AND fechaModificacion >= DATEADD(hour, -24, GETDATE())
GROUP BY estado
ORDER BY cnt DESC;";

    // -------- ERROR BACKLOG (errores aún presentes) + edad máxima --------

    public const string IntraErrorMaxAgeMin7d = @"
SELECT CAST(ISNULL(MAX(DATEDIFF(MINUTE, fechaOperacion, GETDATE())),0) AS FLOAT)
FROM Transferencia
WHERE estado IN (5,9,15)
  AND fechaOperacion >= DATEADD(day, -7, GETDATE());";

    public const string InterErrorMaxAgeMin7d = @"
SELECT CAST(ISNULL(MAX(DATEDIFF(MINUTE, fechaOperacion, GETDATE())),0) AS FLOAT)
FROM TransferenciaInterbancaria
WHERE estado IN (5,9)
  AND fechaOperacion >= DATEADD(day, -7, GETDATE());";

    // -------- ERRORES POR ESTADO (snapshot actual en ventana) --------

    public const string IntraErrorsByState24h = @"
SELECT estado, COUNT(*) AS cnt
FROM Transferencia
WHERE estado IN (5,9,15)
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE())
GROUP BY estado
ORDER BY cnt DESC;";

    public const string InterErrorsByState24h = @"
SELECT estado, COUNT(*) AS cnt
FROM TransferenciaInterbancaria
WHERE estado IN (5,9)
  AND fechaOperacion >= DATEADD(hour, -24, GETDATE())
GROUP BY estado
ORDER BY cnt DESC;";

    // -------- DRILL DOWN (para debugging; cuidado si tu poller solo espera scalar) --------
    // Útil para logs/inspección manual. NO lo metas como "métrica scalar".

    public const string InterRecentErrorsTop50_24h = @"
SELECT TOP (50)
  idTransferenciaInterbancaria,
  estado,
  fechaOperacion,
  fechaModificacion,
  mensajeError
FROM TransferenciaInterbancaria
WHERE estado IN (5,9)
  AND fechaModificacion >= DATEADD(hour, -24, GETDATE())
ORDER BY fechaModificacion DESC;";
}