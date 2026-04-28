using ORYS.WebApi.Database;

var builder = WebApplication.CreateBuilder(args);

// CORS: Herhangi bir kaynaktan istek kabul et (local dev + network)
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

// DB ayarlarını configuration'dan al
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


var app = builder.Build();

// DB tablolarını başlatırken online_reservations tablosunu oluştur
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<DbConnectionFactory>();
    dbFactory.InitializeOnlineTable();
}

// Portu ortam değişkeninden al (Render vb. platformlar için)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5050";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// ... (DB tablosu baslatma kodlari ayni kaldi) ...

app.UseCors("AllowAll");

// Statik dosya sunumu
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

// Kök URL ve diğer tüm rotaları index.html'e yönlendir (SPA desteği)
app.MapFallbackToFile("index.html");

app.Run();
