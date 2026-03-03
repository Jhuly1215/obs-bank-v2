using Serilog;

namespace Bank.Obs.DemoApi.Logging;

public static class SerilogExtensions
{
    public static LoggerConfiguration EnrichWithStandardProperties(this LoggerConfiguration cfg)
        => cfg.Enrich.FromLogContext()
              .Enrich.WithProperty("service", "bank-obs-demo-api");
}