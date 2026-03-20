using System.Threading.Tasks;

namespace Bank.Obs.DemoApi.Models;

public class TransferenciaInternaDto
{
    public string CodigoAgenda { get; set; } = string.Empty;
    public string CuentaOrigen { get; set; } = string.Empty;
    public string CuentaDestino { get; set; } = string.Empty;
    public int TipoTransferencia { get; set; }
    public decimal Monto { get; set; }
    public int CodigoMoneda { get; set; }
    public string Glosa { get; set; } = string.Empty;
    public int TipoVerificacion { get; set; }
    public string CodigoVerificacion { get; set; } = string.Empty;
    public AuditoriaDispositivoDto AuditoriaDispositivo { get; set; } = new();
}

public class TransferenciaInterbancariaDto
{
    public string CodigoAgenda { get; set; } = string.Empty;
    public string CuentaOrigen { get; set; } = string.Empty;
    public int BancoDestinoId { get; set; }
    public DestinatarioDto Destinatario { get; set; } = new();
    public int TipoTransferencia { get; set; }
    public decimal Monto { get; set; }
    public int CodigoMoneda { get; set; }
    public string Glosa { get; set; } = string.Empty;
    public AuditoriaDispositivoDto AuditoriaDispositivo { get; set; } = new();
}

public class DestinatarioDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Nit { get; set; } = string.Empty;
    public string Cuenta { get; set; } = string.Empty;
}

public class AuditoriaDispositivoDto
{
    public string Dispositivo { get; set; } = string.Empty;
    public string TipoPersona { get; set; } = string.Empty;
    public string Latitud { get; set; } = string.Empty;
    public string Longitud { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
}

public class TransferenciaResultadoDto
{
    public long? TransferenciaId { get; set; }
    public long? InterbancariaId { get; set; }
    public int Estado { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
