using System;
using System.Threading.Tasks;
using Bank.Obs.FcmBridge.Data;
using Bank.Obs.FcmBridge.Models.Requests;
using Bank.Obs.FcmBridge.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Obs.FcmBridge.Controllers;

[ApiController]
[Route("v1/usuarios-notificacion")]
// El lineamiento pide que estén protegidos.
// Asumiendo que la validación de API Key se hace con algún atributo o middleware como [Authorize(AuthenticationSchemes = "ApiKey")]
// o simplemente validando el header.
// Aquí usamos [Authorize] genérico, pero asumiendo que el pipeline lo maneja o se debe validar el header manualmente si no hay un scheme.
[Authorize] 
public class UsuariosNotificacionController : ControllerBase
{
    private readonly IUsuariosNotificacionRepositorio _repositorio;

    public UsuariosNotificacionController(IUsuariosNotificacionRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarUsuarioNotificacionRequest request)
    {
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
    public async Task<IActionResult> CambiarEstado([FromBody] CambiarEstadoUsuarioNotificacionRequest request)
    {
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
    public async Task<IActionResult> ObtenerTodos()
    {
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
    public async Task<IActionResult> ListarActivos()
    {
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
