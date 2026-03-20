using Bank.Obs.FcmBridge.Models;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace Bank.Obs.FcmBridge.Services;

public class FcmService : IFcmService
{
    private readonly ILogger<FcmService> _logger;

    public FcmService(ILogger<FcmService> logger)
    {
        _logger = logger;
    }

    public async Task<(bool Success, string Topic, string? MessageId, string? ErrorMessage)> SendAlertAsync(GrafanaWebhookPayload payload)
    {
        try
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                _logger.LogError("FirebaseApp no está inicializado. Falta el service-account.json o la configuración de credenciales.");
                return (false, string.Empty, null, "Firebase no configurado");
            }

            // Mapeo de Severidad -> Topic FCM
            var severity = ExtractSeverity(payload);
            var topic = MapSeverityToTopic(severity);

            // Construcción del Mensaje FCM (Data Payload para Android)
            var message = new Message()
            {
                Data = new Dictionary<string, string>()
                {
                    { "title", payload.Title ?? "Nueva Alerta" },
                    { "body", payload.Message ?? "Sin detalle." },
                    { "severity", severity }
                },
                Topic = topic
            };

            // Enviar a FCM
            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("Mensaje enviado a FCM exitosamente. Topic: {topic}, MessageId: {id}", topic, response);
            
            return (true, topic, response, null);
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "Error del servidor de Firebase al enviar notificación FCM.");
            return (false, string.Empty, null, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crítico interno al procesar/enviar a FCM.");
            return (false, string.Empty, null, ex.Message);
        }
    }

    private string ExtractSeverity(GrafanaWebhookPayload payload)
    {
        if (payload.CommonLabels != null && payload.CommonLabels.TryGetValue("severity", out var extractedSeverity))
        {
            return extractedSeverity.ToLowerInvariant();
        }
        return "info"; // default
    }

    private string MapSeverityToTopic(string severity)
    {
        return severity switch
        {
            "critical" => "obsbank-critical",
            "warning" => "obsbank-warning",
            _ => "obsbank-info"
        };
    }
}
