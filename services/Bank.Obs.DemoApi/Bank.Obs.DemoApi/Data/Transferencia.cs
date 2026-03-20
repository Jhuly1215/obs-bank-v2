using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bank.Obs.DemoApi.Data;

[Table("Transferencia")]
public class Transferencia
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("transferenciaID")]
    public long TransferenciaId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("codigoAgenda")]
    public string CodigoAgenda { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("cuentaOrigen")]
    public string CuentaOrigen { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("cuentaDestino")]
    public string CuentaDestino { get; set; } = string.Empty;

    [Required]
    [Column("tipoTransferencia")]
    public int TipoTransferencia { get; set; }

    [Required]
    [Column("monto", TypeName = "money")]
    public decimal Monto { get; set; }

    [Required]
    [Column("codigoMoneda")]
    public int CodigoMoneda { get; set; }

    [Required]
    [MaxLength(300)]
    [Column("glosa")]
    public string Glosa { get; set; } = string.Empty;

    [MaxLength(300)]
    [Column("fondosOrigen")]
    public string? FondosOrigen { get; set; }

    [MaxLength(300)]
    [Column("fondosDestino")]
    public string? FondosDestino { get; set; }

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
}
