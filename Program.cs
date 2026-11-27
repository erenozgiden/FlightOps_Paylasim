using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using FlightOps.Data;
using FlightOps.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.WebEncoders;

const string Url = "http://localhost:5080";

Console.OutputEncoding = Encoding.UTF8;

// Tarih, sayı ve para biçimleri Türkçe olsun
var turkce = new CultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = turkce;
CultureInfo.DefaultThreadCurrentUICulture = turkce;

// Komut satırından veritabanını MySQL .sql dosyası olarak dışa aktarma:
//   dotnet run -- --sql-disa-aktar Veritabani/flightops_mysql.sql           (uygulamadaki güncel veriler)
//   dotnet run -- --sql-disa-aktar Veritabani/flightops_mysql.sql --ornek   (yalnızca örnek veriler)
int disaAktarIndeks = Array.IndexOf(args, "--sql-disa-aktar");
bool disaAktarmaModu = disaAktarIndeks >= 0;

if (!disaAktarmaModu)
{
    Console.Title = "FlightOps";

    // Uygulama zaten açıksa ikinci kez başlatma, sadece tarayıcıyı aç
    if (await ZatenCalisiyor())
    {
        TarayiciAc();
        return;
    }
}

// Masaüstü kısayolundan açılınca dosyaları (wwwroot, appsettings.json) exe'nin yanında ara
var exeKlasoru = AppContext.BaseDirectory;
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = disaAktarmaModu ? Array.Empty<string>() : args,
    ContentRootPath = Directory.Exists(Path.Combine(exeKlasoru, "wwwroot"))
        ? exeKlasoru
        : Directory.GetCurrentDirectory(),
});
builder.WebHost.UseUrls(Url);

// Konsol penceresine her SQL komutunu yazmasın, sadece uyarı ve hataları göstersin
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);

// ---- Servisler ----

// MVC: Controller + View; tüm formlarda sahte istek (CSRF) koruması
builder.Services.AddControllersWithViews(o =>
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// ---- Veritabanı: appsettings.json -> "Veritabani:Tur" = "Sqlite" (varsayılan) veya "MySql" ----
var veri = new VeriKlasoru(); // SQLite dosyası ve yüklenen fotoğraflar: %LOCALAPPDATA%\FlightOps
builder.Services.AddSingleton(veri);

bool mySql = string.Equals(builder.Configuration["Veritabani:Tur"], "MySql", StringComparison.OrdinalIgnoreCase);
string mySqlBaglanti = builder.Configuration["Veritabani:MySqlBaglanti"] ?? "";
ServerVersion? mySqlSurumu = null;
string veritabaniBilgisi;

if (mySql)
{
    try
    {
        // Sunucuya bir kez bağlanıp MySQL / MariaDB sürümünü öğren
        mySqlSurumu = ServerVersion.AutoDetect(mySqlBaglanti);
    }
    catch (Exception ex)
    {
        BaglantiHatasi(ex);
        return;
    }
    var b = new MySqlConnector.MySqlConnectionStringBuilder(mySqlBaglanti);
    veritabaniBilgisi = $"MySQL {mySqlSurumu.Version} - {b.Server}:{b.Port} / {b.Database}";
}
else
{
    veri.EskiVerileriTasi(
        builder.Environment.ContentRootPath,
        exeKlasoru,
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "FlightOps"));
    veritabaniBilgisi = $"SQLite - {veri.Veritabani}";
}

builder.Services.AddDbContext<FlightOpsDbContext>(o =>
{
    if (mySql)
        o.UseMySql(mySqlBaglanti, mySqlSurumu!);
    else
        o.UseSqlite($"Data Source={veri.Veritabani}");
    o.ReplaceService<IModelCacheKeyFactory, SaglayiciyaGoreModelAnahtari>();
});

// Çerez ile oturum ve rol bazlı yetki (Admin / Personel / Kullanici)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Hesap/Giris";
        o.LogoutPath = "/Hesap/Cikis";
        o.AccessDeniedPath = "/Hesap/ErisimEngellendi";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();

builder.Services.AddSingleton<FotoServisi>();

// Sayfa kaynağında Türkçe karakterler (ç, ş, ğ ...) olduğu gibi yazılsın
builder.Services.Configure<WebEncoderOptions>(o =>
    o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

var app = builder.Build();

// Veritabanını oluştur (yoksa) ve ilk açılışta örnek verileri ekle
try
{
    using var kapsam = app.Services.CreateScope();
    BaslangicVerisi.Ekle(kapsam.ServiceProvider.GetRequiredService<FlightOpsDbContext>());
}
catch (Exception ex) when (mySql)
{
    BaglantiHatasi(ex);
    return;
}

if (disaAktarmaModu)
{
    await SqlDisaAktar(app, args, disaAktarIndeks);
    return;
}

// ---- İstek hattı ----

app.UseStaticFiles();
// Yüklenen fotoğraflar kalıcı veri klasöründen /yuklemeler adresiyle sunulur
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(veri.Yuklemeler),
    RequestPath = "/yuklemeler"
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Hesap}/{action=Index}/{id?}");

// Sunucu hazır olunca tarayıcıyı aç (appsettings.json -> "Uygulama:TarayiciyiAc")
app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine($"FlightOps çalışıyor: {Url}");
    Console.WriteLine($"Veritabanı: {veritabaniBilgisi}");
    Console.WriteLine("Kapatmak için bu pencereyi kapatın.");
    if (app.Configuration.GetValue("Uygulama:TarayiciyiAc", true))
        TarayiciAc();
});

app.Run();

// ---- Yardımcılar ----

static async Task SqlDisaAktar(WebApplication app, string[] args, int indeks)
{
    if (indeks + 1 >= args.Length || args[indeks + 1].StartsWith("--"))
    {
        Console.WriteLine("Kullanım: dotnet run -- --sql-disa-aktar <dosya.sql> [--ornek]");
        return;
    }

    var hedef = Path.GetFullPath(args[indeks + 1]);
    string sql;

    if (args.Contains("--ornek"))
    {
        // Geçici, boş bir SQLite veritabanı oluşturup yalnızca örnek verilerle doldur
        var gecici = Path.Combine(Path.GetTempPath(), $"flightops-ornek-{Guid.NewGuid():N}.db");
        var secenekler = new DbContextOptionsBuilder<FlightOpsDbContext>()
            .UseSqlite($"Data Source={gecici};Pooling=False")
            .ReplaceService<IModelCacheKeyFactory, SaglayiciyaGoreModelAnahtari>()
            .Options;
        try
        {
            await using var db = new FlightOpsDbContext(secenekler);
            BaslangicVerisi.Ekle(db);
            sql = await MySqlDisaAktarici.UretAsync(db, "Örnek veriler (uygulamanın ilk açılışta eklediği kayıtlar)");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(gecici);
        }
    }
    else
    {
        using var kapsam = app.Services.CreateScope();
        var db = kapsam.ServiceProvider.GetRequiredService<FlightOpsDbContext>();
        sql = await MySqlDisaAktarici.UretAsync(db, "Uygulamadaki güncel veriler");
    }

    Directory.CreateDirectory(Path.GetDirectoryName(hedef)!);
    await File.WriteAllTextAsync(hedef, sql, new UTF8Encoding(false));
    Console.WriteLine($"MySQL dışa aktarma dosyası yazıldı: {hedef}");
}

static void BaglantiHatasi(Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("MySQL veritabanına bağlanılamadı.");
    Console.WriteLine($"Hata: {ex.GetBaseException().Message}");
    Console.WriteLine();
    Console.WriteLine("Kontrol edin:");
    Console.WriteLine("  1) MySQL sunucusu çalışıyor mu? (örn. XAMPP Control Panel -> MySQL -> Start)");
    Console.WriteLine("  2) Veritabani/flightops_mysql.sql dosyası MySQL'e içe aktarıldı mı?");
    Console.WriteLine("  3) appsettings.json içindeki \"MySqlBaglanti\" (sunucu, port, kullanıcı, şifre) doğru mu?");
    Console.WriteLine("SQLite ile çalıştırmak için appsettings.json'da \"Tur\": \"Sqlite\" yapın.");
    Console.WriteLine();
    Console.WriteLine("Kapatmak için bir tuşa basın...");
    try { Console.ReadKey(true); } catch { /* konsol yoksa beklemeden çık */ }
    Environment.ExitCode = 1;
}

static void TarayiciAc()
{
    try { Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true }); }
    catch { /* tarayıcı açılamazsa adresi elle açın */ }
}

static async Task<bool> ZatenCalisiyor()
{
    try
    {
        using var istemci = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
        return (await istemci.GetAsync(Url)).IsSuccessStatusCode;
    }
    catch
    {
        return false;
    }
}
