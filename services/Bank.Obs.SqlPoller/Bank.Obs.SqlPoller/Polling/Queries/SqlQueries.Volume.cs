namespace Bank.Obs.SqlPoller.Polling;

public static partial class SqlQueries
{
    // =========================
    // VOLUMEN / ACTIVIDAD (registro observado)
    // =========================

    // ---- 15 minutos (latido) ----

    public const string IntraTxLast15m = @"
SELECT COUNT(*)
FROM Transferencia
WHERE fechaOperacion >= DATEADD(minute, -15, GETDATE());";

    public const string InterTxLast15m = @"
SELECT COUNT(*)
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(minute, -15, GETDATE());";

    // ---- 24 horas ----

    public const string IntraTxLast24h = @"
SELECT COUNT(*)
FROM Transferencia
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE());";

    public const string InterTxLast24h = @"
SELECT COUNT(*)
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE());";

    // ---- 30 días ----

    public const string IntraTxLast30d = @"
SELECT COUNT(*)
FROM Transferencia
WHERE fechaOperacion >= DATEADD(day, -30, GETDATE());";

    public const string InterTxLast30d = @"
SELECT COUNT(*)
FROM TransferenciaInterbancaria
WHERE fechaOperacion >= DATEADD(day, -30, GETDATE());";
}