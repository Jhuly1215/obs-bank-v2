namespace Bank.Obs.FcmBridge.Options;

public class FcmBridgeOptions
{
    public int tamanioLoteEnvio { get; set; } = 500;
    public bool habilitarDiagnosticoUsuarios { get; set; } = false;
    public string? apiKey { get; set; }
}
