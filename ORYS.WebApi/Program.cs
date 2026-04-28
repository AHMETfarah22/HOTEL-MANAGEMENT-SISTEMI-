using ORYS.WebApi.Database;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Kök dizindeki config.json dosyasini oku
builder.Configuration.AddJsonFile("../config.json", optional: true, reloadOnChange: true);

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
