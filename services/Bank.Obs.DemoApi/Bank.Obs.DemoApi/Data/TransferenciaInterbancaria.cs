using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bank.Obs.DemoApi.Data;

[Table("TransferenciaInterbancaria")]
public class TransferenciaInterbancaria
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("InterbancariaId")]
    public long InterbancariaId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("codigoAgenda")]
    public string CodigoAgenda { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("cuentaOrigen")]
    public string? CuentaOrigen { get; set; }

    [Column("monto", TypeName = "money")]
    public decimal? Monto { get; set; }

    [Column("monedaOperacion")]
    public int? MonedaOperacion { get; set; }

    [Column("bancoDestino")]
    public int? BancoDestino { get; set; }

    [MaxLength(10)]
    [Column("cuentaDestinoTipo")]
    public string? CuentaDestinoTipo { get; set; }

    [MaxLength(200)]
    [Column("destinatarioNombre")]
    public string? DestinatarioNombre { get; set; }

    [MaxLength(40)]
    [Column("destinatarioNIT")]
    public string? DestinatarioNIT { get; set; }

    [MaxLength(20)]
    [Column("destinatarioCuenta")]
    public string? DestinatarioCuenta { get; set; }

    [MaxLength(300)]
    [Column("glosa")]
    public string? Glosa { get; set; }

    [MaxLength(300)]
    [Column("fondosOrigen")]
    public string? FondosOrigen { get; set; }

    [MaxLength(300)]
    [Column("fondosDestino")]
    public string? FondosDestino { get; set; }

    [Required]
    [Column("tipoTransferencia")]
    public int TipoTransferencia { get; set; }

    [Column("tipoVerificacion")]
    public int? TipoVerificacion { get; set; }

    [MaxLength(10)]
    [Column("codigoVerificacion")]
    public string? CodigoVerificacion { get; set; }

    [Required]
    [Column("estado")]
    public int Estado { get; set; }

    [MaxLength(100)]
    [Column("numeroAuditoria")]
    public string? NumeroAuditoria { get; set; }

    [MaxLength(80)]
    [Column("numeroControl")]
    public string? NumeroControl { get; set; }

    [Column("tipoCambioMonto", TypeName = "money")]
    public decimal? TipoCambioMonto { get; set; }

    [MaxLength(10)]
    [Column("requierePCC1")]
    public string? RequierePCC1 { get; set; }

    [Column("fechaOperacion")]
    public DateTime? FechaOperacion { get; set; }

    [Column("fechaModificacion")]
    public DateTime? FechaModificacion { get; set; }

    [MaxLength(255)]
    [Column("mensajeError")]
    public string? MensajeError { get; set; }

    [MaxLength(4)]
    [Column("dispositivo")]
    public string? Dispositivo { get; set; }

    [MaxLength(2)]
    [Column("tipoPersona")]
    public string? TipoPersona { get; set; }

    [MaxLength(50)]
    [Column("favorito")]
    public string? Favorito { get; set; }

    [MaxLength(100)]
    [Column("cuentaTitular")]
    public string? CuentaTitular { get; set; }

    [Column("mediador")]
    public int? Mediador { get; set; }

    [Column("transaccionOrigen")]
    public int? TransaccionOrigen { get; set; }

    [MaxLength(30)]
    [Column("latitud")]
    public string? Latitud { get; set; }

    [MaxLength(30)]
    [Column("longitud")]
    public string? Longitud { get; set; }

    [MaxLength(250)]
    [Column("pais")]
    public string? Pais { get; set; }

    [MaxLength(250)]
    [Column("ciudad")]
    public string? Ciudad { get; set; }

    [MaxLength(250)]
    [Column("zona")]
    public string? Zona { get; set; }
}
