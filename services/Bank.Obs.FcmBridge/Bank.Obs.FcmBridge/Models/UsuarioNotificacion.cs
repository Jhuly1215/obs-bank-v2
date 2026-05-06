namespace Bank.Obs.FcmBridge.Models;

public sealed class UsuarioNotificacion
{
    public long id { get; set; }
    public string codigoAgenda { get; set; } = string.Empty;
    public string correo { get; set; } = string.Empty;
    public bool estado { get; set; }
    public DateTime fechaCreacion { get; set; }
    public string usuarioCreacion { get; set; } = string.Empty;
    public DateTime? fechaModificacion { get; set; }
    public string? usuarioModificacion { get; set; }
}
