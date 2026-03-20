using Bank.Obs.FcmBridge.Models;

namespace Bank.Obs.FcmBridge.Services;

public interface IFcmService
{
    Task<(bool Success, string Topic, string? MessageId, string? ErrorMessage)> SendAlertAsync(GrafanaWebhookPayload payload);
}
