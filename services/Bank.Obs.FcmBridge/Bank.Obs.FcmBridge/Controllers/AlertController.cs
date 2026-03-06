using Bank.Obs.FcmBridge.Models;
using Bank.Obs.FcmBridge.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Obs.FcmBridge.Controllers;

[ApiController]
[Route("")]
public class AlertController : ControllerBase
{
    private readonly IFcmService _fcmService;
    private readonly ILogger<AlertController> _logger;
    private readonly string _apiKey;

    public AlertController(IFcmService fcmService, ILogger<AlertController> logger, IConfiguration configuration)
    {
        _fcmService = fcmService;
        _logger = logger;
        _apiKey = configuration["BRIDGE_API_KEY"] ?? "DEV_INSECURE_KEY_REPLACE_ME";
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "UP", service = "obs-bank-fcm-bridge" });
    }

    [HttpPost("alert")]
    public async Task<IActionResult> ReceiveAlert([FromHeader(Name = "Authorization")] string? authorization, [FromBody] GrafanaWebhookPayload payload)
    {
        // 1. Autorización Básica mediante ApiKey (Bearer)
        if (string.IsNullOrWhiteSpace(authorization) || authorization != $"Bearer {_apiKey}")
        {
            _logger.LogWarning("Intento de acceso denegado en /alert. Auth header inválido o inexistente.");
            return Unauthorized(new { error = "Acceso denegado" });
        }

        if (payload == null)
        {
            return BadRequest(new { error = "Payload inválido o vacío." });
        }

        _logger.LogInformation("Recibida alerta de Grafana: {title}", payload.Title);

        // 2. Procesar con el Servicio FCM
        var result = await _fcmService.SendAlertAsync(payload);

        if (result.Success)
        {
            return Ok(new { success = true, topic = result.Topic, messageId = result.MessageId });
        }

        return StatusCode(500, new { error = "Fallo interno al despachar alerta a Firebase", details = result.ErrorMessage });
    }
}
