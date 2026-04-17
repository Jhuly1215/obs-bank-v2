namespace Bank.Obs.SqlPoller.Polling;

public static partial class SqlQueries
{
    // ===================================
    // HEALTH & INFRASTRUCTURE
    // ===================================

    // 1) Day Type Detection (Agnostic to @@DATEFIRST)
    public const string GetDayType = @"
DECLARE @Today INT = DATEPART(dw, GETDATE());
DECLARE @Sunday INT = DATEPART(dw, '2023-01-01');
DECLARE @Saturday INT = DATEPART(dw, '2023-01-07');
SELECT CASE WHEN @Today IN (@Sunday, @Saturday) THEN 'fin_de_semana' ELSE 'habil' END";

    // 2) Server Sessions & Waits (Corrected from PDF inconsistency)
    public const string ServerSessions = @"
SELECT 
    status AS [Status], 
    ISNULL(wait_type, 'NONE') AS WaitType, 
    COUNT(1) AS [Count]
FROM sys.dm_exec_requests 
WHERE session_id > 50 
GROUP BY status, wait_type";

    // 3) Real File I/O (Corrected from PDF inconsistency)
    public const string FileIoStats = @"
SELECT 
    file_id AS FileId, 
    io_stall_read_ms AS ReadStallMs, 
    io_stall_write_ms AS WriteStallMs,
    num_of_bytes_read AS BytesRead,
    num_of_bytes_written AS BytesWritten
FROM sys.dm_io_virtual_file_stats(DB_ID(), NULL)";

    // 4) Database File Size & Usage
    public const string DatabaseSize = @"
SELECT 
    name AS FileName, 
    size * 8 / 1024 AS SizeMB, 
    FILEPROPERTY(name, 'SpaceUsed') * 8 / 1024 AS UsedMB 
FROM sys.database_files";

    // 5) Data Quality Anomalies (Grouped)
    public const string QualityAnomalies = @"
SELECT 
    fuente AS Source, 
    tipoTransferencia AS Tipo, 
    estado AS Estado,
    SUM(CASE WHEN fechaModificacion IS NULL AND estado IN (3, 8) THEN 1 ELSE 0 END) AS MissingMod,
    SUM(CASE WHEN DATEDIFF(second, fechaOperacion, fechaModificacion) = 0 AND estado IN (3, 8) THEN 1 ELSE 0 END) AS ZeroDur,
    SUM(CASE WHEN DATEDIFF(second, fechaOperacion, fechaModificacion) < 0 AND estado IN (3, 8) THEN 1 ELSE 0 END) AS NegDur
FROM (
    SELECT 'intra' as fuente, ISNULL(CAST(tipoTransferencia AS INT), 0) as tipoTransferencia, estado, fechaOperacion, fechaModificacion 
    FROM Transferencia 
    WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE())
    UNION ALL
    SELECT 'inter' as fuente, ISNULL(CAST(tipoTransferencia AS INT), 0) as tipoTransferencia, estado, fechaOperacion, fechaModificacion 
    FROM TransferenciaInterbancaria 
    WHERE fechaOperacion >= DATEADD(hour, -24, GETDATE())
) AllTx
GROUP BY fuente, tipoTransferencia, estado";
}
