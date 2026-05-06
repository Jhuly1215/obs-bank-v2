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
        var dicTokens = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        int batchSize = 500;
        for (int i = 0; i < codigosAgenda.Count; i += batchSize)
        {
            var batch = codigosAgenda.Skip(i).Take(batchSize).ToList();
            var parameterNames = batch.Select((c, index) => $"@p{index}").ToList();
            var inClause = string.Join(", ", parameterNames);

            var query = $@"
                SELECT 
                    codigoAgenda, 
                    tokenNotificacion 
                FROM Econet.dbo.UsuariosNotificacion
                WHERE estado = 1
                  AND codigoAgenda IN ({inClause})
                  AND tokenNotificacion IS NOT NULL
                  AND LTRIM(RTRIM(tokenNotificacion)) <> '';
            ";

            using var command = new SqlCommand(query, connection);
            for (int j = 0; j < batch.Count; j++)
            {
                command.Parameters.AddWithValue(parameterNames[j], batch[j]);
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var codigo = reader.GetString(reader.GetOrdinal("codigoAgenda"));
                var token = reader.GetString(reader.GetOrdinal("tokenNotificacion"));
                
                if (!dicTokens.ContainsKey(codigo))
                {
                    dicTokens[codigo] = new List<string>();
                }
                dicTokens[codigo].Add(token);
            }
        }

        foreach (var usuario in usuarios)
        {
            if (dicTokens.TryGetValue(usuario.codigoAgenda, out var tokens))
            {
                foreach (var token in tokens)
                {
                    result.Add(new TokenNotificacionUsuario
                    {
                        codigoAgenda = usuario.codigoAgenda,
                        correo = usuario.correo,
                        tokenNotificacion = token
                    });
                }
            }
        }

        return result;
    }
}
