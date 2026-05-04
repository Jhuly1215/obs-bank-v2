using System;
using System.Threading.Tasks;
using Bank.Obs.FcmBridge.Data;
using Bank.Obs.FcmBridge.Models.Requests;
using Bank.Obs.FcmBridge.Models.Responses;
using Bank.Obs.FcmBridge.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bank.Obs.FcmBridge.Controllers;

[ApiController]
[Route("v1/usuarios-notificacion")]
public class UsuariosNotificacionController : ControllerBase
{
    private readonly IUsuariosNotificacionRepositorio _repositorio;
    private readonly ILogger<UsuariosNotificacionController> _logger;
    private readonly FcmBridgeOptions _options;

    public UsuariosNotificacionController(
        IUsuariosNotificacionRepositorio repositorio,
        ILogger<UsuariosNotificacionController> logger,
        IOptions<FcmBridgeOptions> options)
    {
        _repositorio = repositorio;
        _logger = logger;
        _options = options.Value;
    }

    private bool ValidarAutorizacion(string? authorization)
    {
        string apiKey = string.IsNullOrWhiteSpace(_options.apiKey) ? Environment.GetEnvironmentVariable("BRIDGE_API_KEY") ?? "DEV_INSECURE_KEY_REPLACE_ME" : _options.apiKey;
        
        if (!string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            authorization = authorization.Substring("Bearer ".Length).Trim();
        }

        return !string.IsNullOrWhiteSpace(authorization) && authorization == apiKey;
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromHeader(Name = "Authorization")] string? authorization, [FromBody] RegistrarUsuarioNotificacionRequest request)
    {
        if (!ValidarAutorizacion(authorization))
        {
            _logger.LogWarning("Intento de acceso denegado en Registrar usuario. Auth header inválido o inexistente.");
            return Unauthorized(new { error = "Acceso denegado" });
        }

        if (string.IsNullOrWhiteSpace(request.codigoAgenda) || string.IsNullOrWhiteSpace(request.correo))
        {
            return BadRequest(new ResultadoUsuarioNotificacion
            {
                success = false,
                message = "codigoAgenda y correo son obligatorios."
            });
        }

        try
        {
            await _repositorio.registrarUsuarioNotificacionAsync(request, "fcm-bridge");
            return Ok(new ResultadoUsuarioNotificacion { success = true, message = "Usuario registrado/actualizado exitosamente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ResultadoUsuarioNotificacion { success = false, message = ex.Message });
        }
    }

    [HttpPatch("estado")]
    public async Task<IActionResult> CambiarEstado([FromHeader(Name = "Authorization")] string? authorization, [FromBody] CambiarEstadoUsuarioNotificacionRequest request)
    {
        if (!ValidarAutorizacion(authorization))
        {
            _logger.LogWarning("Intento de acceso denegado en CambiarEstado. Auth header inválido o inexistente.");
            return Unauthorized(new { error = "Acceso denegado" });
        }

        if (string.IsNullOrWhiteSpace(request.codigoAgenda))
        {
            return BadRequest(new ResultadoUsuarioNotificacion
            {
                success = false,
                message = "codigoAgenda es obligatorio."
            });
        }

        try
        {
            await _repositorio.cambiarEstadoUsuarioNotificacionAsync(request, "fcm-bridge");
            return Ok(new ResultadoUsuarioNotificacion { success = true, message = "Estado modificado exitosamente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ResultadoUsuarioNotificacion { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromHeader(Name = "Authorization")] string? authorization)
    {
        if (!ValidarAutorizacion(authorization))
        {
            _logger.LogWarning("Intento de acceso denegado en ObtenerTodos. Auth header inválido o inexistente.");
            return Unauthorized(new { error = "Acceso denegado" });
        }

        try
        {
            var usuarios = await _repositorio.obtenerUsuariosNotificacionAsync();
            return Ok(usuarios);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("activos")]
    public async Task<IActionResult> ListarActivos([FromHeader(Name = "Authorization")] string? authorization)
    {
        if (!ValidarAutorizacion(authorization))
        {
            _logger.LogWarning("Intento de acceso denegado en ListarActivos. Auth header inválido o inexistente.");
            return Unauthorized(new { error = "Acceso denegado" });
        }

        try
        {
            var usuarios = await _repositorio.listarUsuariosNotificacionActivosAsync();
            return Ok(usuarios);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
