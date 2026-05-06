namespace Bank.Obs.FcmBridge.Models.Requests;

public sealed class RegistrarUsuarioNotificacionRequest
{
    public string codigoAgenda { get; set; } = string.Empty;
    public string correo { get; set; } = string.Empty;
}
