using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bank.Obs.FcmBridge.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Bank.Obs.FcmBridge.Data;

public sealed class SqlTokensNotificacionRepositorio : ITokensNotificacionRepositorio
{
    private readonly string _connectionString;

    public SqlTokensNotificacionRepositorio(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("EconetDb") 
            ?? throw new ArgumentNullException(nameof(configuration), "ConnectionString EconetDb no configurado.");
    }

    public async Task<IReadOnlyList<TokenNotificacionUsuario>> obtenerTokensPorCodigosAgendaAsync(IReadOnlyList<UsuarioNotificacion> usuarios)
    {
        var result = new List<TokenNotificacionUsuario>();

        if (usuarios == null || usuarios.Count == 0)
        {
            return result;
        }

        var codigosAgenda = usuarios.Select(u => u.codigoAgenda).Distinct().ToList();

        // Building the parameter list for the IN clause
        var parameterNames = codigosAgenda.Select((c, i) => $"@p{i}").ToList();
        var inClause = string.Join(", ", parameterNames);
        
        // PENDIENTE REAL: Confirmar el nombre exacto de la tabla de Econet que contiene:
        // - codigoAgenda (o CodigoAgenda)
        // - TokenNotificacion
        // Placeholder table name: Econet.dbo.TokensAppMovil
        var query = $@"
            SELECT 
                CodigoAgenda AS codigoAgenda, 
                TokenNotificacion 
            FROM dbo.TokensAppMovil WITH (NOLOCK)
            WHERE CodigoAgenda IN ({inClause})
              AND TokenNotificacion IS NOT NULL
              AND LTRIM(RTRIM(TokenNotificacion)) <> '';
        ";

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(query, connection);

        for (int i = 0; i < codigosAgenda.Count; i++)
        {
            command.Parameters.AddWithValue(parameterNames[i], codigosAgenda[i]);
        }

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();

        var dicTokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        while (await reader.ReadAsync())
        {
            var codigoAgenda = reader.GetString(reader.GetOrdinal("codigoAgenda"));
            var tokenNotificacion = reader.GetString(reader.GetOrdinal("TokenNotificacion"));
            // Keep the first token encountered per code or build a list if required. Overwriting for now.
            dicTokens[codigoAgenda] = tokenNotificacion;
        }

        foreach (var usuario in usuarios)
        {
            if (dicTokens.TryGetValue(usuario.codigoAgenda, out var token))
            {
                result.Add(new TokenNotificacionUsuario
                {
                    codigoAgenda = usuario.codigoAgenda,
                    correo = usuario.correo,
                    tokenNotificacion = token
                });
            }
        }

        return result;
    }
}
