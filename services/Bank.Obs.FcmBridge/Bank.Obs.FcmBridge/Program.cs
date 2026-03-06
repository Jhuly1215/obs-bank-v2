using Bank.Obs.FcmBridge.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

var builder = WebApplication.CreateBuilder(args);

// 1. Añadir Servicios al Contenedor (Dependency Injection)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IFcmService, FcmService>();

var app = builder.Build();

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
