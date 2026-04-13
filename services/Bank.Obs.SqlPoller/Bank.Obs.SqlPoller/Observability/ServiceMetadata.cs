namespace Bank.Obs.SqlPoller.Observability;

public sealed record ServiceMetadata(string Name, string Version, Uri OtlpEndpoint)
{
    public static ServiceMetadata FromConfiguration(IConfiguration config)
    {
        var endpoint = config["OpenTelemetry:OtlpEndpoint"] ?? "http://otel-collector:4317";
        var name = config["OpenTelemetry:ServiceName"] ?? "bank-sql-poller";
        var version = config["OpenTelemetry:ServiceVersion"] ?? "2.0.0";

        return new ServiceMetadata(name, version, new Uri(endpoint));
    }
}
