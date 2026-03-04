namespace Bank.Obs.SqlPoller.Polling;

public static partial class SqlQueries
{
    // =========================
    // INTERBANCARIO (semántica parcial)
    // Regla: estado 9 = error
    // =========================

    // ---- 30d ----

    public const string InterSuccessRate30d = @"
SELECT CAST(1.0 * SUM(CASE WHEN estado = 3 THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS FLOAT)
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(day, -30, GETDATE());";

    public const string InterFailTechRate30d = @"
SELECT CAST(1.0 * SUM(CASE WHEN estado IN (5,9) THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS FLOAT)
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(day, -30, GETDATE());";

    public const string InterErrorState9Share30d = @"
SELECT CAST(1.0 * SUM(CASE WHEN estado = 9 THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS FLOAT)
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(day, -30, GETDATE());";

    public const string InterUnknownStateCount30d = @"
SELECT COUNT(*)
FROM TransferenciaInterbancaria
WHERE estado NOT IN (1,3,5,6,9)
  AND fechaOperacion >= DATEADD(day, -30, GETDATE());";

    // ---- 24h ----

    public const string InterSuccessRate24h = @"
SELECT CAST(1.0 * SUM(CASE WHEN estado = 3 THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS FLOAT)
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE());";

    public const string InterFailTechRate24h = @"
SELECT CAST(1.0 * SUM(CASE WHEN estado IN (5,9) THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS FLOAT)
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE());";

    public const string InterErrorState9Share24h = @"
SELECT CAST(1.0 * SUM(CASE WHEN estado = 9 THEN 1 ELSE 0 END) / NULLIF(COUNT(*),0) AS FLOAT)
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE());";
}