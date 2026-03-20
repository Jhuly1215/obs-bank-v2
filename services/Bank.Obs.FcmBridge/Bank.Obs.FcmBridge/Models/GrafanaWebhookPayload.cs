using System.Text.Json.Serialization;

namespace Bank.Obs.FcmBridge.Models;

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
