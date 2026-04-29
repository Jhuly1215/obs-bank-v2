using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bank.Obs.FcmBridge.Data;
using Bank.Obs.FcmBridge.Models;
using Bank.Obs.FcmBridge.Models.Responses;
using Bank.Obs.FcmBridge.Options;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bank.Obs.FcmBridge.Services;

public class FcmService : IFcmService
{
    private readonly ILogger<FcmService> _logger;
    private readonly IUsuariosNotificacionRepositorio _usuariosRepo;
    private readonly ITokensNotificacionRepositorio _tokensRepo;
    private readonly FcmBridgeOptions _options;

    public FcmService(
        ILogger<FcmService> logger, 
        IUsuariosNotificacionRepositorio usuariosRepo,
        ITokensNotificacionRepositorio tokensRepo,
        IOptions<FcmBridgeOptions> options)
    {
        _logger = logger;
        _usuariosRepo = usuariosRepo;
        _tokensRepo = tokensRepo;
        _options = options.Value;
    }

    public async Task<ResultadoEnvioNotificacion> enviarAlertaAsync(GrafanaWebhookPayload payload)
    {
        var result = new ResultadoEnvioNotificacion();

        try
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                _logger.LogError("FirebaseApp no está inicializado. Falta el service-account.json o la configuración de credenciales.");
                result.success = false;
                return result;
            }

            // 1. Consultar usuarios activos en EcoMonitor
            var usuariosActivos = await _usuariosRepo.listarUsuariosNotificacionActivosAsync();
            result.totalUsuariosActivos = usuariosActivos.Count;

            if (usuariosActivos.Count == 0)
            {
                _logger.LogInformation("No hay usuarios activos configurados para recibir alertas.");
                result.success = true;
                return result;
            }

            // 2. Consultar tokens en Econet
            var tokensUsuario = await _tokensRepo.obtenerTokensPorCodigosAgendaAsync(usuariosActivos);
            result.totalTokensEncontrados = tokensUsuario.Count;

            // 3. Detectar usuarios sin token
            var codigosConToken = new HashSet<string>(tokensUsuario.Select(t => t.codigoAgenda));
            foreach (var usuario in usuariosActivos)
            {
                if (!codigosConToken.Contains(usuario.codigoAgenda))
                {
                    result.usuariosSinToken.Add(new UsuarioSinToken
                    {
                        codigoAgenda = usuario.codigoAgenda,
                        correo = usuario.correo
                    });
                }
            }

            // 4. Filtrar y deduplicar tokens válidos
            var validTokensList = tokensUsuario
                .Where(t => !string.IsNullOrWhiteSpace(t.tokenNotificacion))
                .GroupBy(t => t.tokenNotificacion)
                .Select(g => g.First())
                .ToList();

            result.totalTokensValidos = validTokensList.Count;

            if (validTokensList.Count == 0)
            {
                _logger.LogInformation("Ningún usuario activo tiene un token de notificación válido.");
                result.success = true;
                return result;
            }

            // 5. Preparar notificación y data
            var severity = ExtractLabel(payload, "severity", "info");

            var notification = new Notification
            {
                Title = payload.Title ?? "Nueva Alerta",
                Body = payload.Message ?? "Sin detalle."
            };

            var dataPayload = new Dictionary<string, string>
            {
                { "type", "obsbank_alert" },
                { "source", "grafana" },
                { "severity", severity },
                { "status", payload.Status ?? payload.State ?? "firing" },
                { "service", ExtractLabel(payload, "service") },
                { "alertName", ExtractLabel(payload, "alertname", payload.AlertName ?? "Alerta Desconocida") },
                { "dashboardUrl", ExtractAnnotation(payload, "dashboardUrl", ExtractAnnotation(payload, "dashboard")) },
                { "panelUrl", ExtractAnnotation(payload, "panelUrl", ExtractAnnotation(payload, "panel")) }
            };

            // 6. Dividir en lotes y enviar
            int batchSize = _options.tamanioLoteEnvio > 0 ? _options.tamanioLoteEnvio : 500;
            var tokensToProcess = validTokensList.Select(t => t.tokenNotificacion).ToList();

            for (int i = 0; i < tokensToProcess.Count; i += batchSize)
            {
                var batchTokens = tokensToProcess.Skip(i).Take(batchSize).ToList();

                var message = new MulticastMessage
                {
                    Tokens = batchTokens,
                    Notification = notification,
                    Data = dataPayload
                };

                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);

                result.successCount += response.SuccessCount;
                result.failureCount += response.FailureCount;

                if (response.FailureCount > 0)
                {
                    for (int j = 0; j < response.Responses.Count; j++)
                    {
                        if (!response.Responses[j].IsSuccess)
                        {
                            var error = response.Responses[j].Exception;
                            var invalidToken = batchTokens[j];
                            string motivo = error?.MessagingErrorCode.ToString() ?? "Unknown";

                            // Encontrar el usuario correspondiente al token fallido
                            var usuarioAfectado = validTokensList.FirstOrDefault(t => t.tokenNotificacion == invalidToken);
                            
                            if (usuarioAfectado != null)
                            {
                                result.tokensFallidos.Add(new TokenNotificacionFallido
                                {
                                    codigoAgenda = usuarioAfectado.codigoAgenda,
                                    correo = usuarioAfectado.correo,
                                    motivo = motivo
                                });
                            }

                            _logger.LogWarning(error, "Error al enviar al token {Token}. Motivo: {Motivo}", invalidToken, motivo);
                        }
                    }
                }
            }

            result.success = true;
            _logger.LogInformation("Envío completado. Activos: {Activos}, Tokens: {Tokens}, Éxitos: {Exitos}, Fallos: {Fallos}",
                result.totalUsuariosActivos, result.totalTokensValidos, result.successCount, result.failureCount);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crítico interno al procesar/enviar a FCM.");
            result.success = false;
            return result;
        }
    }

    private string ExtractLabel(GrafanaWebhookPayload payload, string key, string defaultValue = "")
    {
        if (payload.CommonLabels != null && payload.CommonLabels.TryGetValue(key, out var value))
        {
            return value;
        }
        return defaultValue;
    }

    private string ExtractAnnotation(GrafanaWebhookPayload payload, string key, string defaultValue = "")
    {
        if (payload.CommonAnnotations != null && payload.CommonAnnotations.TryGetValue(key, out var value))
        {
            return value;
        }
        return defaultValue;
    }
}
