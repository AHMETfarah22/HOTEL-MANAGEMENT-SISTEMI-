using ORYS.WebApi.Database;
using Microsoft.Extensions.Configuration;

// .env dosyasından çevre değişkenlerini yükle
LoadEnvFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"));
LoadEnvFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", ".env"));
LoadEnvFile(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

// Kök dizindeki config.json dosyasini oku
builder.Configuration.AddJsonFile("../config.json", optional: true, reloadOnChange: true);

// Çevre değişkenlerinden MailSettings'i override et
var emailUser = Environment.GetEnvironmentVariable("EMAIL_USER");
var emailPass = Environment.GetEnvironmentVariable("EMAIL_APP_PASSWORD");
var emailHost = Environment.GetEnvironmentVariable("EMAIL_HOST");
var emailPort = Environment.GetEnvironmentVariable("EMAIL_PORT");

if (!string.IsNullOrEmpty(emailUser))
    builder.Configuration["MailSettings:Mail"] = emailUser;
if (!string.IsNullOrEmpty(emailPass))
    builder.Configuration["MailSettings:Password"] = emailPass;
if (!string.IsNullOrEmpty(emailHost))
    builder.Configuration["MailSettings:Host"] = emailHost;
if (!string.IsNullOrEmpty(emailPort))
    builder.Configuration["MailSettings:Port"] = emailPort;

// Çevre değişkenlerinden Database ayarlarını override et
var dbServer = Environment.GetEnvironmentVariable("DB_SERVER");
var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPass = Environment.GetEnvironmentVariable("DB_PASSWORD");

if (!string.IsNullOrEmpty(dbServer))
    builder.Configuration["Database:Server"] = dbServer;
if (!string.IsNullOrEmpty(dbPort))
    builder.Configuration["Database:Port"] = dbPort;
if (!string.IsNullOrEmpty(dbName))
    builder.Configuration["Database:DatabaseName"] = dbName;
if (!string.IsNullOrEmpty(dbUser))
    builder.Configuration["Database:UserId"] = dbUser;
if (!string.IsNullOrEmpty(dbPass))
    builder.Configuration["Database:Password"] = dbPass;
// CORS: Herhangi bir kaynaktan istek kabul et
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// DB ayarlarini configuration'dan al
builder.Services.AddSingleton<DbConnectionFactory>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new DbConnectionFactory(
        config["Database:Server"] ?? "localhost",
        config["Database:Port"] ?? "3306",
        config["Database:DatabaseName"] ?? "orys_db",
        config["Database:UserId"] ?? "root",
        config["Database:Password"] ?? ""
    );
});

// Mail Servisini Kaydet
builder.Services.AddScoped<ORYS.WebApi.Services.IMailService, ORYS.WebApi.Services.MailService>();

// PDF Servisini Kaydet
builder.Services.AddScoped<ORYS.WebApi.Services.IPdfService, ORYS.WebApi.Services.PdfService>();

// Arka Plan Servisleri
builder.Services.AddHostedService<ORYS.WebApi.Services.ReservationCleanupService>();

// Portu ortam değişkeninden al (Render vb. platformlar için)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5050";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// DB tablolarini baslat
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<DbConnectionFactory>();
    dbFactory.InitializeOnlineTable();
}

app.UseCors("AllowAll");

// Statik dosya sunumu
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

// Kök URL ve diğer tüm rotaları index.html'e yönlendir (SPA desteği)
app.MapFallbackToFile("index.html");

app.Run();

// =============================================
// .env dosyası okuyucu - Hassas bilgileri güvenle yükler
// =============================================
static void LoadEnvFile(string path)
{
    if (!File.Exists(path)) return;
    
    foreach (var line in File.ReadAllLines(path))
    {
        var trimmed = line.Trim();
        
        // Boş satırları ve yorumları atla
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
            continue;
        
        var eqIndex = trimmed.IndexOf('=');
        if (eqIndex <= 0) continue;
        
        var key = trimmed[..eqIndex].Trim();
        var value = trimmed[(eqIndex + 1)..].Trim();
        
        // Sadece henüz tanımlanmamış değişkenleri yükle (sistem ortam değişkenleri öncelikli)
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
