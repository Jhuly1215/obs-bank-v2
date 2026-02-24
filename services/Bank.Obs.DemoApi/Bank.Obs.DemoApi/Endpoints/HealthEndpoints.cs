using Bank.Obs.DemoApi.Observability;

namespace Bank.Obs.DemoApi.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealth(this IEndpointRouteBuilder app, ServiceMetadata meta)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok", service = meta.Name, version = meta.Version }))
           .WithName("Health");

        return app;
    }
}