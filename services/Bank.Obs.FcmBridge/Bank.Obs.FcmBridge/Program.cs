using Bank.Obs.FcmBridge.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Añadir Servicios al Contenedor (Dependency Injection)
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
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddSingleton<IFcmService, FcmService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bank.Obs.FcmBridge API V1");
});

// 2. Configuración de Firebase Admin SDK
var credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
if (string.IsNullOrEmpty(credentialsPath) || !File.Exists(credentialsPath))
{
    app.Logger.LogWarning("GOOGLE_APPLICATION_CREDENTIALS no está configurado o el archivo no existe en la ruta: {path}", credentialsPath);
    app.Logger.LogWarning("El servicio de Firebase no podrá enviar notificaciones hasta que se monte el secreto.");
}
else
{
    FirebaseApp.Create(new AppOptions()
    {
        Credential = GoogleCredential.FromFile(credentialsPath)
    });
    app.Logger.LogInformation("Firebase Admin SDK inicializado exitosamente.");
}

// 3. Middlewares y Ruteo
app.UseRouting();
app.UseAuthorization();
app.MapControllers(); // Habilita los [ApiController] como nuestro AlertController

app.Run();
