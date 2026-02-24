using Bank.Obs.DemoApi.Endpoints;
using Bank.Obs.DemoApi.Middleware;
using Bank.Obs.DemoApi.Observability;
using Bank.Obs.DemoApi.Services;

var builder = WebApplication.CreateBuilder(args);

var meta = ServiceMetadata.FromConfiguration(builder.Configuration);

builder.Services.AddHttpClient();

// Swagger (requiere Swashbuckle.AspNetCore en csproj)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Options + Services
builder.Services.Configure<TransferSimulatorOptions>(builder.Configuration.GetSection("TransferSimulator"));
builder.Services.AddSingleton<ITransferService, TransferService>();

// Middleware
builder.Services.AddSingleton<CorrelationIdMiddleware>();

// OpenTelemetry
builder.AddObservability(meta);

var app = builder.Build();

// Swagger solo en Development (no lo expongas siempre)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationIdMiddleware>();

app.MapHealth(meta);
app.MapTransactions();

app.Run();