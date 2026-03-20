using Bank.Obs.DemoApi.Auth;
using Bank.Obs.DemoApi.Endpoints;
using Bank.Obs.DemoApi.Middleware;
using Bank.Obs.DemoApi.Observability;
using Bank.Obs.DemoApi.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Serilog.Formatting.Compact;
using System.Threading.RateLimiting;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;
using Bank.Obs.DemoApi.Data;

var builder = WebApplication.CreateBuilder(args);

// --- Logging (Serilog structured JSON) ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

// --- Configuration ---
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                     .AddEnvironmentVariables();

// --- Security: Require HTTPS in Prod + HSTS handled below ---
if (builder.Environment.IsProduction())
{
    builder.WebHost.UseUrls("https://*:5001");
}

// --- Auth ---
// Dev: auth �fake� para probar [Authorize] sin IdP real (requiere header Authorization).
// Prod: JwtBearer real.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication("Dev")
        .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>("Dev", _ => { });
}
else
{
    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", options =>
        {
            options.Authority = builder.Configuration["Auth:Authority"];
            options.Audience = builder.Configuration["Auth:Audience"];
            options.RequireHttpsMetadata = true;
        });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanTransfer", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("scope", "transactions.transfer"));
});

// --- Rate Limiter ---
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("transfer", limiterOptions =>
    {
        limiterOptions.PermitLimit = 30;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});

// --- FluentValidation ---
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// --- Entity Framework Core Context ---
var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_CONN") ?? builder.Configuration.GetConnectionString("EconetConnection");
builder.Services.AddDbContext<EconetDbContext>(op => op.UseSqlServer(connectionString));

// --- Idempotency store (demo in-memory) ---
var idempProvider = builder.Configuration["Idempotency:Provider"] ?? "InMemory";
var ttlSeconds = builder.Configuration.GetValue<int>("Idempotency:TtlSeconds");
var ttl = TimeSpan.FromSeconds(ttlSeconds <= 0 ? 3600 : ttlSeconds);

builder.Services.AddSingleton(new IdempotencyTtl(TimeSpan.FromMinutes(10)));

if (idempProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
{
    var redisConn = builder.Configuration["Idempotency:RedisConnectionString"] ?? "redis:6379";
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
    builder.Services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();
}
else
{
    builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
}

// --- HttpClient & Metadata ---
var meta = ServiceMetadata.FromConfiguration(builder.Configuration);
builder.Services.AddHttpClient();

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Bank Obs Demo API", Version = "v1" });

    // Para Development: permitir meter Authorization header desde Swagger UI
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Dev: escribe 'Dev test' o 'Bearer <token>'"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// --- Options + Services ---
builder.Services.Configure<TransferSimulatorOptions>(builder.Configuration.GetSection("TransferSimulator"));
builder.Services.AddSingleton<ITransferService, TransferService>();

// --- OpenTelemetry ---
builder.AddObservability(meta);

var app = builder.Build();

// --- Swagger (solo Development) ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- Middlewares (order matters) ---
app.UseSerilogRequestLogging();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// --- Endpoints ---
app.MapHealth(meta);
app.MapControllers();

app.Run();

