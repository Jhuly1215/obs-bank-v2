using System.Collections.Generic;

namespace Bank.Obs.FcmBridge.Models.Responses;

public sealed class ResultadoEnvioNotificacion
{
    public bool success { get; set; }
    public int totalUsuariosActivos { get; set; }
    public int totalTokensEncontrados { get; set; }
    public int totalTokensValidos { get; set; }
    public int successCount { get; set; }
    public int failureCount { get; set; }
    public List<UsuarioSinToken> usuariosSinToken { get; set; } = new();
    public List<TokenNotificacionFallido> tokensFallidos { get; set; } = new();
}
