using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Bank.Obs.FcmBridge.Models;
using Bank.Obs.FcmBridge.Models.Requests;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Bank.Obs.FcmBridge.Data;

public sealed class SqlUsuariosNotificacionRepositorio : IUsuariosNotificacionRepositorio
{
    private readonly string _connectionString;

    public SqlUsuariosNotificacionRepositorio(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("EcoMonitorDb") 
            ?? throw new ArgumentNullException(nameof(configuration), "ConnectionString EcoMonitorDb no configurado.");
    }

    public async Task registrarUsuarioNotificacionAsync(RegistrarUsuarioNotificacionRequest solicitud, string usuarioOperacion)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.spRegistrarUsuarioNotificacion", connection);
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@codigoAgenda", solicitud.codigoAgenda);
        command.Parameters.AddWithValue("@correo", solicitud.correo);
        command.Parameters.AddWithValue("@usuarioOperacion", usuarioOperacion);

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    public async Task cambiarEstadoUsuarioNotificacionAsync(CambiarEstadoUsuarioNotificacionRequest solicitud, string usuarioOperacion)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.spCambiarEstadoUsuarioNotificacion", connection);
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@codigoAgenda", solicitud.codigoAgenda);
        command.Parameters.AddWithValue("@estado", solicitud.estado);
        command.Parameters.AddWithValue("@usuarioOperacion", usuarioOperacion);

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<UsuarioNotificacion>> listarUsuariosNotificacionActivosAsync()
    {
        var result = new List<UsuarioNotificacion>();

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.spListarUsuariosNotificacionActivos", connection);
        command.CommandType = CommandType.StoredProcedure;

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new UsuarioNotificacion
            {
                codigoAgenda = reader.GetString(reader.GetOrdinal("codigoAgenda")),
                correo = reader.GetString(reader.GetOrdinal("correo"))
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<UsuarioNotificacion>> obtenerUsuariosNotificacionAsync()
    {
        var result = new List<UsuarioNotificacion>();

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.spObtenerUsuariosNotificacion", connection);
        command.CommandType = CommandType.StoredProcedure;

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new UsuarioNotificacion
            {
                id = reader.GetInt64(reader.GetOrdinal("id")),
                codigoAgenda = reader.GetString(reader.GetOrdinal("codigoAgenda")),
                correo = reader.GetString(reader.GetOrdinal("correo")),
                estado = reader.GetBoolean(reader.GetOrdinal("estado")),
                fechaCreacion = reader.GetDateTime(reader.GetOrdinal("fechaCreacion")),
                usuarioCreacion = reader.GetString(reader.GetOrdinal("usuarioCreacion")),
                fechaModificacion = reader.IsDBNull(reader.GetOrdinal("fechaModificacion")) ? null : reader.GetDateTime(reader.GetOrdinal("fechaModificacion")),
                usuarioModificacion = reader.IsDBNull(reader.GetOrdinal("usuarioModificacion")) ? null : reader.GetString(reader.GetOrdinal("usuarioModificacion"))
            });
        }

        return result;
    }
}
