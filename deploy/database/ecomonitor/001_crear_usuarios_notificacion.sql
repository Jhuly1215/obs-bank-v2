USE [EcoMonitor];
GO

IF OBJECT_ID('dbo.UsuariosNotificacion', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuariosNotificacion (
        id BIGINT IDENTITY(1,1) NOT NULL,
        codigoAgenda VARCHAR(20) NOT NULL,
        correo VARCHAR(150) NOT NULL,

        estado BIT NOT NULL
            CONSTRAINT dfUsuariosNotificacionEstado DEFAULT 1,

        fechaCreacion DATETIME2(3) NOT NULL
            CONSTRAINT dfUsuariosNotificacionFechaCreacion DEFAULT SYSUTCDATETIME(),

        usuarioCreacion VARCHAR(100) NOT NULL
            CONSTRAINT dfUsuariosNotificacionUsuarioCreacion DEFAULT 'sistema',

        fechaModificacion DATETIME2(3) NULL,
        usuarioModificacion VARCHAR(100) NULL,

        CONSTRAINT pkUsuariosNotificacion
            PRIMARY KEY CLUSTERED (id),

        CONSTRAINT uqUsuariosNotificacionCodigoAgenda
            UNIQUE (codigoAgenda),

        CONSTRAINT ckUsuariosNotificacionEstado
            CHECK (estado IN (0, 1))
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'ixUsuariosNotificacionEstado'
      AND object_id = OBJECT_ID('dbo.UsuariosNotificacion')
)
BEGIN
    CREATE INDEX ixUsuariosNotificacionEstado
    ON dbo.UsuariosNotificacion (estado);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'ixUsuariosNotificacionCodigoAgenda'
      AND object_id = OBJECT_ID('dbo.UsuariosNotificacion')
)
BEGIN
    CREATE INDEX ixUsuariosNotificacionCodigoAgenda
    ON dbo.UsuariosNotificacion (codigoAgenda);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'ixUsuariosNotificacionCorreo'
      AND object_id = OBJECT_ID('dbo.UsuariosNotificacion')
)
BEGIN
    CREATE INDEX ixUsuariosNotificacionCorreo
    ON dbo.UsuariosNotificacion (correo);
END;
GO

-- Stored Procedures

-- 1. spRegistrarUsuarioNotificacion
IF OBJECT_ID('dbo.spRegistrarUsuarioNotificacion', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spRegistrarUsuarioNotificacion;
GO

CREATE PROCEDURE dbo.spRegistrarUsuarioNotificacion
    @codigoAgenda VARCHAR(20),
    @correo VARCHAR(150),
    @usuarioOperacion VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (SELECT 1 FROM dbo.UsuariosNotificacion WHERE codigoAgenda = @codigoAgenda)
        BEGIN
            UPDATE dbo.UsuariosNotificacion
            SET correo = @correo,
                estado = 1,
                fechaModificacion = SYSUTCDATETIME(),
                usuarioModificacion = @usuarioOperacion
            WHERE codigoAgenda = @codigoAgenda;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.UsuariosNotificacion (codigoAgenda, correo, estado, usuarioCreacion)
            VALUES (@codigoAgenda, @correo, 1, @usuarioOperacion);
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

-- 2. spCambiarEstadoUsuarioNotificacion
IF OBJECT_ID('dbo.spCambiarEstadoUsuarioNotificacion', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spCambiarEstadoUsuarioNotificacion;
GO

CREATE PROCEDURE dbo.spCambiarEstadoUsuarioNotificacion
    @codigoAgenda VARCHAR(20),
    @estado BIT,
    @usuarioOperacion VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.UsuariosNotificacion
        SET estado = @estado,
            fechaModificacion = SYSUTCDATETIME(),
            usuarioModificacion = @usuarioOperacion
        WHERE codigoAgenda = @codigoAgenda;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

-- 3. spListarUsuariosNotificacionActivos
IF OBJECT_ID('dbo.spListarUsuariosNotificacionActivos', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spListarUsuariosNotificacionActivos;
GO

CREATE PROCEDURE dbo.spListarUsuariosNotificacionActivos
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        codigoAgenda, 
        correo
    FROM dbo.UsuariosNotificacion
    WHERE estado = 1;
END;
GO

-- 4. spObtenerUsuariosNotificacion
IF OBJECT_ID('dbo.spObtenerUsuariosNotificacion', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spObtenerUsuariosNotificacion;
GO

CREATE PROCEDURE dbo.spObtenerUsuariosNotificacion
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        id,
        codigoAgenda,
        correo,
        estado,
        fechaCreacion,
        usuarioCreacion,
        fechaModificacion,
        usuarioModificacion
    FROM dbo.UsuariosNotificacion;
END;
GO
