namespace Bank.Obs.FcmBridge.Models.Responses;

public sealed class TokenNotificacionFallido
{
    public string codigoAgenda { get; set; } = string.Empty;
    public string correo { get; set; } = string.Empty;
    public string motivo { get; set; } = string.Empty;
}
