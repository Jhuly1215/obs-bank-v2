using Bank.Obs.FcmBridge.Middleware;
using Bank.Obs.FcmBridge.Observability;
using Bank.Obs.FcmBridge.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// --- Logging (Serilog structured JSON) ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

// --- Metadata ---
var meta = ServiceMetadata.FromConfiguration(builder.Configuration);

// --- Observability (OpenTelemetry) ---
builder.AddObservability(meta);

// --- Services ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Bank.Obs.FcmBridge API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa tu token en el formato: Bearer secreto_123_cambiar_en_produccion"
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            []
        }
    });
});

builder.Services.Configure<Bank.Obs.FcmBridge.Options.FcmBridgeOptions>(
    builder.Configuration.GetSection("FcmBridge"));

builder.Services.AddScoped<Bank.Obs.FcmBridge.Data.IUsuariosNotificacionRepositorio, Bank.Obs.FcmBridge.Data.SqlUsuariosNotificacionRepositorio>();
builder.Services.AddScoped<Bank.Obs.FcmBridge.Data.ITokensNotificacionRepositorio, Bank.Obs.FcmBridge.Data.SqlTokensNotificacionRepositorio>();
builder.Services.AddScoped<IFcmService, FcmService>();

var app = builder.Build();

// --- Middlewares & Routing ---
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bank.Obs.FcmBridge API V1");
});

// --- Firebase Admin SDK ---
var credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
if (string.IsNullOrEmpty(credentialsPath) || !File.Exists(credentialsPath))
{
    app.Logger.LogWarning("GOOGLE_APPLICATION_CREDENTIALS no está configurado o el archivo no existe en la ruta: {path}", credentialsPath);
}
else
{
    try
    {
        FirebaseApp.Create(new AppOptions()
        {
            Credential = GoogleCredential.FromFile(credentialsPath)
        });
        app.Logger.LogInformation("Firebase Admin SDK inicializado exitosamente.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error al inicializar Firebase Admin SDK.");
    }
}

app.UseAuthorization();
app.MapControllers();

app.Run();
