using Bank.Obs.FcmBridge.Models;
using Bank.Obs.FcmBridge.Models.Responses;

namespace Bank.Obs.FcmBridge.Services;

public interface IFcmService
{
    Task<ResultadoEnvioNotificacion> enviarAlertaAsync(GrafanaWebhookPayload payload);
}
