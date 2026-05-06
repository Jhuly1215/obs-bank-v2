namespace Bank.Obs.FcmBridge.Models.Requests;

public sealed class CambiarEstadoUsuarioNotificacionRequest
{
    public string codigoAgenda { get; set; } = string.Empty;
    public bool estado { get; set; }
}
