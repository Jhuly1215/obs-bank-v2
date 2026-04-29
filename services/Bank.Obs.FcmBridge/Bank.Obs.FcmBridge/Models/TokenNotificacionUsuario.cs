namespace Bank.Obs.FcmBridge.Models;

public sealed class TokenNotificacionUsuario
{
    public string codigoAgenda { get; set; } = string.Empty;
    public string correo { get; set; } = string.Empty;
    public string tokenNotificacion { get; set; } = string.Empty;
}
