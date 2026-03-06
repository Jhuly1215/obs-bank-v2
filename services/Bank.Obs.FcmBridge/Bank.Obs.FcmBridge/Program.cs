using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 1. Configuración de Firebase Admin SDK
// Se espera que la variable de entorno GOOGLE_APPLICATION_CREDENTIALS apunte al service-account.json
var credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
var apiKey = Environment.GetEnvironmentVariable("BRIDGE_API_KEY") ?? "DEV_INSECURE_KEY_REPLACE_ME";

if (string.IsNullOrEmpty(credentialsPath) || !File.Exists(credentialsPath))
{
    app.Logger.LogWarning("GOOGLE_APPLICATION_CREDENTIALS no está configurado o el archivo no existe en la ruta: {path}", credentialsPath);
    app.Logger.LogWarning("El servicio de Firebase no podrá enviar notificaciones hasta que se monte el secreto.");
}
else
{
    FirebaseApp.Create(new AppOptions()
    {
        Credential = GoogleCredential.FromFile(credentialsPath)
    });
    app.Logger.LogInformation("Firebase Admin SDK inicializado exitosamente.");
}

// 2. Healthcheck simple
app.MapGet("/health", () => Results.Ok(new { status = "UP", service = "obs-bank-fcm-bridge" }));

// 3. Endpoint principal webhook
app.MapPost("/alert", async ([FromHeader(Name = "Authorization")] string? authorization, [FromBody] GrafanaWebhookPayload payload, ILogger<Program> logger) =>
{
    // A) Validación estática de API KEY
    if (string.IsNullOrWhiteSpace(authorization) || authorization != $"Bearer {apiKey}")
    {
        logger.LogWarning("Intento de acceso denegado en /alert. Auth header inválido.");
        return Results.Unauthorized();
    }

    if (payload == null)
    {
        return Results.BadRequest("Payload inválido.");
    }

    logger.LogInformation("Recibida alerta de Grafana: {title}", payload.Title);

    // B) Mapeo de Severidad -> Topic FCM
    var severity = "info"; // Default
    if (payload.CommonLabels != null && payload.CommonLabels.TryGetValue("severity", out var extractedSeverity))
    {
        severity = extractedSeverity.ToLowerInvariant();
    }

    var topic = severity switch
    {
        "critical" => "obsbank-critical",
        "warning" => "obsbank-warning",
        _ => "obsbank-info"
    };

    // C) Construcción del Mensaje FCM (Solo Data)
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

    // D) Enviar usando Firebase Admin SDK
    try
    {
        if (FirebaseApp.DefaultInstance != null)
        {
            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            logger.LogInformation("Mensaje enviado a FCM exitosamente. Topic: {topic}, MessageId: {id}", topic, response);
            return Results.Ok(new { success = true, topic, messageId = response });
        }
        else
        {
            logger.LogError("FirebaseApp no está inicializado. Falta el service-account.json.");
            return Results.StatusCode(500); // Internal Server Error si Firebase no está arriba.
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error crítico al enviar a FCM.");
        return Results.StatusCode(500);
    }
});

app.Run();

// ============================================
// DTOs para parsear el Webhook de Grafana
// ============================================
public class GrafanaWebhookPayload
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("commonLabels")]
    public Dictionary<string, string>? CommonLabels { get; set; }
}
