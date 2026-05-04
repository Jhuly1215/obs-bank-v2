using Bank.Obs.FcmBridge.Models;
using Bank.Obs.FcmBridge.Models.Responses;
using Bank.Obs.FcmBridge.Options;
using Bank.Obs.FcmBridge.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bank.Obs.FcmBridge.Controllers;

[ApiController]
[Route("")]
public class AlertController : ControllerBase
{
    private readonly IFcmService _fcmService;
    private readonly ILogger<AlertController> _logger;
    private readonly FcmBridgeOptions _options;

    public AlertController(IFcmService fcmService, ILogger<AlertController> logger, IOptions<FcmBridgeOptions> options)
    {
        _fcmService = fcmService;
        _logger = logger;
        _options = options.Value;
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "UP", service = "obs-bank-fcm-bridge" });
    }

    [HttpPost("alert")]
    public async Task<IActionResult> ReceiveAlert([FromHeader(Name = "Authorization")] string? authorization, [FromBody] GrafanaWebhookPayload payload)
    {
        string apiKey = string.IsNullOrWhiteSpace(_options.apiKey) ? Environment.GetEnvironmentVariable("BRIDGE_API_KEY") ?? "DEV_INSECURE_KEY_REPLACE_ME" : _options.apiKey;

        if (!string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            authorization = authorization.Substring("Bearer ".Length).Trim();
        }

        if (string.IsNullOrWhiteSpace(authorization) || authorization != apiKey)
        {
            _logger.LogWarning("Intento de acceso denegado en /alert. Auth header inválido o inexistente.");
            return Unauthorized(new { error = "Acceso denegado" });
        }

        if (payload == null)
        {
            return BadRequest(new { error = "Payload inválido o vacío." });
        }

        _logger.LogInformation("Recibida alerta de Grafana: {title}", payload.Title);

        var result = await _fcmService.enviarAlertaAsync(payload);

        if (result.success)
        {
            _logger.LogInformation("Alerta procesada. Activos: {Activos}, Validos: {Validos}, Éxitos: {Success}, Fallos: {Failure}", 
                result.totalUsuariosActivos, result.totalTokensValidos, result.successCount, result.failureCount);
            
            return Ok(new { 
                success = true, 
                totalUsuariosActivos = result.totalUsuariosActivos,
                totalTokensEncontrados = result.totalTokensEncontrados,
                totalTokensValidos = result.totalTokensValidos,
                successCount = result.successCount, 
                failureCount = result.failureCount, 
                usuariosSinToken = result.usuariosSinToken,
                tokensFallidos = result.tokensFallidos 
            });
        }

        _logger.LogError("Error al procesar alerta para FCM.");
        return StatusCode(500, new { error = "Fallo interno al despachar alerta a Firebase" });
    }
}
