namespace Bank.Obs.DemoApi.Services;

public sealed class TransferSimulatorOptions
{
    // Probabilidad de fallo (0.0 a 1.0)
    public double FailureRate { get; set; } = 0.15;

    // Latencia simulada
    public int MinLatencyMs { get; set; } = 50;
    public int MaxLatencyMs { get; set; } = 250;

    // Código de error cuando falla
    public string ErrorCode { get; set; } = "INTERBANK_TIMEOUT";
}