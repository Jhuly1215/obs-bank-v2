using Bank.Obs.DemoApi.Models;
using Bank.Obs.DemoApi.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Bank.Obs.DemoApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TransferenciasController : ControllerBase
{
    private readonly EconetDbContext _dbContext;
    private readonly ILogger<TransferenciasController> _logger;

    public TransferenciasController(EconetDbContext dbContext, ILogger<TransferenciasController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost("internas")]
    public async Task<IActionResult> RegistrarTransferenciaInterna([FromBody] TransferenciaInternaDto request)
    {
        // Validación inicial. Ej: Mismo banco.
        if (request.CuentaOrigen == request.CuentaDestino)
            return BadRequest(new { Mensaje = "Cuenta origen y destino no pueden ser iguales para terceros, o verificar el tipo." });

        _logger.LogInformation("Iniciando registro de transferencia interna. CuentaOrigen: {CuentaOrigen}, Monto: {Monto}", request.CuentaOrigen, request.Monto);

        try
        {
            var entidad = new Transferencia
            {
                TransferenciaId = long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")),
                CodigoAgenda = request.CodigoAgenda,
                CuentaOrigen = request.CuentaOrigen,
                CuentaDestino = request.CuentaDestino,
                TipoTransferencia = request.TipoTransferencia,
                Monto = request.Monto,
                CodigoMoneda = request.CodigoMoneda,
                Glosa = request.Glosa,
                TipoVerificacion = request.TipoVerificacion,
                CodigoVerificacion = request.CodigoVerificacion,
                Estado = 1, // 1 = Solicitada
                FechaOperacion = DateTime.UtcNow,
                Dispositivo = request.AuditoriaDispositivo?.Dispositivo,
                TipoPersona = request.AuditoriaDispositivo?.TipoPersona
            };

            _dbContext.Transferencias.Add(entidad);
            _logger.LogInformation("Guardando transferencia interna en BD...");
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Transferencia interna guardada correctamente. ID: {Id}", entidad.TransferenciaId);

            var resultado = new TransferenciaResultadoDto
            {
                TransferenciaId = entidad.TransferenciaId,
                Estado = 1,
                Mensaje = "Transferencia interna registrada correctamente."
            };
            
            return CreatedAtAction(nameof(ObtenerTransferenciaStatus), new { id = resultado.TransferenciaId }, resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar transferencia interna en la BD.");
            throw;
        }
    }

    [HttpPost("interbancarias")]
    public async Task<IActionResult> RegistrarTransferenciaInterbancaria([FromBody] TransferenciaInterbancariaDto request)
    {
        _logger.LogInformation("Iniciando registro de transferencia interbancaria. CuentaOrigen: {CuentaOrigen}, BancoDestino: {BancoDestino}, Monto: {Monto}", request.CuentaOrigen, request.BancoDestinoId, request.Monto);

        try
        {
            var entidad = new TransferenciaInterbancaria
            {
                InterbancariaId = long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")),
                CodigoAgenda = request.CodigoAgenda,
                CuentaOrigen = request.CuentaOrigen,
                BancoDestino = request.BancoDestinoId,
                DestinatarioNombre = request.Destinatario?.Nombre,
                DestinatarioNIT = request.Destinatario?.Nit,
                DestinatarioCuenta = request.Destinatario?.Cuenta,
                TipoTransferencia = request.TipoTransferencia,
                Monto = request.Monto,
                Glosa = request.Glosa,
                Estado = 1, // 1 = Solicitada
                FechaOperacion = DateTime.UtcNow,
                Latitud = request.AuditoriaDispositivo?.Latitud,
                Longitud = request.AuditoriaDispositivo?.Longitud,
                Ciudad = request.AuditoriaDispositivo?.Ciudad,
                Dispositivo = request.AuditoriaDispositivo?.Dispositivo
            };

            _dbContext.TransferenciaInterbancarias.Add(entidad);
            _logger.LogInformation("Guardando transferencia interbancaria en BD...");
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Transferencia interbancaria guardada correctamente. ID: {Id}", entidad.InterbancariaId);

            var resultado = new TransferenciaResultadoDto
            {
                InterbancariaId = entidad.InterbancariaId,
                Estado = 1,
                Mensaje = "Transferencia interbancaria registrada correctamente."
            };
            
            return CreatedAtAction(nameof(ObtenerTransferenciaStatus), new { id = resultado.InterbancariaId }, resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar transferencia interbancaria en la BD.");
            throw;
        }
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> ObtenerTransferenciaStatus(long id)
    {
        // Mock de consulta de estado por ID
        // Aquí devuelves el estado, mapeando los IDs de la db (ej: 0, 3, 5, 6...)
        var mockStatus = new 
        {
            Id = id,
            EstadoId = 3,
            EstadoDescripcion = "Transferencia Aceptada"
        };
        
        return Ok(await Task.FromResult(mockStatus));
    }

    [HttpPut("internas/{id}/estado")]
    public async Task<IActionResult> CambiarEstadoInterna(long id, [FromBody] int nuevoEstado)
    {
        var transferencia = await _dbContext.Transferencias.FindAsync(id);
        if (transferencia == null) return NotFound(new { Mensaje = "Transferencia interna no encontrada." });

        int estadoAnterior = transferencia.Estado;
        transferencia.Estado = nuevoEstado;
        transferencia.FechaModificacion = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Estado de transferencia interna {Id} cambiado de {EstadoAnterior} a {NuevoEstado}", id, estadoAnterior, nuevoEstado);

        return Ok(new { Mensaje = "Estado actualizado correctamente.", TransferenciaId = id, NuevoEstado = nuevoEstado });
    }

    [HttpPut("interbancarias/{id}/estado")]
    public async Task<IActionResult> CambiarEstadoInterbancaria(long id, [FromBody] int nuevoEstado)
    {
        var transferencia = await _dbContext.TransferenciaInterbancarias.FindAsync(id);
        if (transferencia == null) return NotFound(new { Mensaje = "Transferencia interbancaria no encontrada." });

        int estadoAnterior = transferencia.Estado;
        transferencia.Estado = nuevoEstado;
        transferencia.FechaModificacion = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Estado de transferencia interbancaria {Id} cambiado de {EstadoAnterior} a {NuevoEstado}", id, estadoAnterior, nuevoEstado);

        return Ok(new { Mensaje = "Estado actualizado correctamente.", InterbancariaId = id, NuevoEstado = nuevoEstado });
    }
}
