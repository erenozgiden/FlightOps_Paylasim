using System.Globalization;
using System.Text;
using FlightOps.Data;
using FlightOps.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FlightOps.Services;

// Veritabanını MySQL'e aktarılabilen TEK bir .sql dosyası olarak üretir:
//   1) Veritabanını oluşturur (CREATE DATABASE flightops)
//   2) Tablo yapısını uygulamanın kendi modelinden (Entity Framework) çıkarır
//   3) Kayıtları INSERT komutları olarak ekler
// Kullanım: Admin > Raporlar > "Veritabanını İndir (.sql)"  veya  dotnet run -- --sql-disa-aktar dosya.sql
public static class MySqlDisaAktarici
{
    public const string VeritabaniAdi = "flightops";

    // Üretilen tablo yapısı MySQL 8 sözdizimiyle yazılır
    private static readonly MySqlServerVersion HedefSurum = new(new Version(8, 0, 36));
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string TabloYapisi()
    {
        var secenekler = new DbContextOptionsBuilder<FlightOpsDbContext>()
            .UseMySql($"Server=localhost;Database={VeritabaniAdi}", HedefSurum)
            .ReplaceService<IModelCacheKeyFactory, SaglayiciyaGoreModelAnahtari>()
            .Options;

        using var db = new FlightOpsDbContext(secenekler);
        return db.Database.GenerateCreateScript().Replace("\r\n", "\n").Trim();
    }

    public static async Task<string> UretAsync(FlightOpsDbContext kaynak, string veriAciklamasi)
    {
        var kullanicilar = await kaynak.Kullanicilar.AsNoTracking().OrderBy(k => k.Id).ToListAsync();
        var ucuslar = await kaynak.Ucuslar.AsNoTracking().OrderBy(u => u.Id).ToListAsync();
        var biletler = await kaynak.Biletler.AsNoTracking().OrderBy(b => b.Id).ToListAsync();

        var s = new StringBuilder();
        s.Append($"""
            -- =====================================================================
            --  FlightOps - Uçuş Yönetim Sistemi
            --  MySQL veritabanı dışa aktarma dosyası (tablo yapısı + kayıtlar)
            --
            --  Oluşturulma : {DateTime.Now.ToString("dd.MM.yyyy HH:mm", Inv)}
            --  Veriler     : {veriAciklamasi}
            --                ({kullanicilar.Count} kullanıcı, {ucuslar.Count} uçuş, {biletler.Count} bilet)
            --  Uyumluluk   : MySQL 8.0.16 ve üzeri (MySQL 8.4 ile denendi)
            --  Karakter    : utf8mb4, Türkçe sıralama (utf8mb4_turkish_ci)
            --
            --  İçe aktarma (dosya "{VeritabaniAdi}" veritabanını kendisi oluşturur):
            --    Komut satırı : mysql -u root -p --default-character-set=utf8mb4 < flightops_mysql.sql
            --    phpMyAdmin   : İçe Aktar sekmesi -> bu dosyayı seçin -> Git
            --    Workbench    : File -> Open SQL Script -> Çalıştır (şimşek simgesi)
            --
            --  Notlar:
            --  - Şifreler düz metin değil, özet (hash) olarak saklanır.
            --  - Kredi kartlarının yalnızca son 4 hanesi saklanır; tam numara ve CVV yoktur.
            --  - Profil fotoğrafları veritabanında değil dosya olarak tutulur; tabloda yalnızca yolları vardır.
            -- =====================================================================

            SET NAMES utf8mb4;
            SET FOREIGN_KEY_CHECKS = 0;

            CREATE DATABASE IF NOT EXISTS `{VeritabaniAdi}`
              DEFAULT CHARACTER SET utf8mb4
              DEFAULT COLLATE utf8mb4_turkish_ci;

            USE `{VeritabaniAdi}`;

            DROP TABLE IF EXISTS `Biletler`;
            DROP TABLE IF EXISTS `Ucuslar`;
            DROP TABLE IF EXISTS `Kullanicilar`;

            -- ---------------------------------------------------------------------
            --  Tablo yapısı
            --  Kullanicilar : admin, personel ve kullanıcılar (Rol: 0 Admin, 1 Personel, 2 Kullanici)
            --  Ucuslar      : uçuşlar
            --  Biletler     : satılan biletler (Ucuslar ve Kullanicilar tablolarına bağlı)
            -- ---------------------------------------------------------------------


            """);
        s.Append(TabloYapisi()).Append("\n\n");

        s.Append("""
            -- ---------------------------------------------------------------------
            --  Kayıtlar
            -- ---------------------------------------------------------------------


            """);

        Ekle(s, "Kullanicilar",
            new[] { "Id", "KullaniciAdi", "SifreHash", "AdSoyad", "Eposta", "Telefon", "Rol", "Gorev", "FotoYolu", "Adres", "KayitTarihi" },
            kullanicilar.Select(k => new[]
            {
                Sayi(k.Id), Metin(k.KullaniciAdi), Metin(k.SifreHash), Metin(k.AdSoyad), Metin(k.Eposta), Metin(k.Telefon),
                Sayi((int)k.Rol), Metin(k.Gorev), Metin(k.FotoYolu), Metin(k.Adres), Tarih(k.KayitTarihi)
            }));

        Ekle(s, "Ucuslar",
            new[] { "Id", "UcusNo", "Kalkis", "Varis", "KalkisZamani", "VarisZamani", "Kapi", "Kapasite", "Fiyat", "Durum" },
            ucuslar.Select(u => new[]
            {
                Sayi(u.Id), Metin(u.UcusNo), Metin(u.Kalkis), Metin(u.Varis), Tarih(u.KalkisZamani), Tarih(u.VarisZamani),
                Metin(u.Kapi), Sayi(u.Kapasite), Para(u.Fiyat), Sayi((int)u.Durum)
            }));

        Ekle(s, "Biletler",
            new[] { "Id", "UcusId", "KullaniciId", "KoltukNo", "Fiyat", "SatinAlmaTarihi", "Durum", "KartSahibi", "KartTipi", "KartSonDortHane" },
            biletler.Select(b => new[]
            {
                Sayi(b.Id), Sayi(b.UcusId), Sayi(b.KullaniciId), Sayi(b.KoltukNo), Para(b.Fiyat), Tarih(b.SatinAlmaTarihi),
                Sayi((int)b.Durum), Metin(b.KartSahibi), Metin(b.KartTipi), Metin(b.KartSonDortHane)
            }));

        s.Append("""
            SET FOREIGN_KEY_CHECKS = 1;

            -- =====================================================================
            --  Örnek sorgular (uygulamadaki ekranların SQL karşılıkları)
            -- =====================================================================

            -- Uçuş bazında satılan bilet, doluluk ve gelir (Admin > Raporlar)
            -- SELECT u.UcusNo,
            --        CONCAT(u.Kalkis, ' -> ', u.Varis)        AS Guzergah,
            --        COUNT(b.Id)                               AS SatilanBilet,
            --        u.Kapasite,
            --        ROUND(COUNT(b.Id) * 100 / u.Kapasite, 1)  AS DolulukYuzde,
            --        IFNULL(SUM(b.Fiyat), 0)                   AS Gelir
            -- FROM Ucuslar u
            -- LEFT JOIN Biletler b ON b.UcusId = u.Id AND b.Durum = 0
            -- GROUP BY u.Id, u.UcusNo, u.Kalkis, u.Varis, u.Kapasite
            -- ORDER BY u.KalkisZamani;

            -- Bir kullanıcının biletleri (Kullanıcı > Biletlerim)
            -- SELECT k.AdSoyad, u.UcusNo, u.Kalkis, u.Varis, u.KalkisZamani, b.KoltukNo, b.Fiyat
            -- FROM Biletler b
            -- JOIN Kullanicilar k ON k.Id = b.KullaniciId
            -- JOIN Ucuslar u      ON u.Id = b.UcusId
            -- WHERE k.KullaniciAdi = 'kullanici'
            -- ORDER BY b.SatinAlmaTarihi DESC;

            -- Rollere göre kullanıcı sayısı
            -- SELECT CASE Rol WHEN 0 THEN 'Admin' WHEN 1 THEN 'Personel' ELSE 'Kullanici' END AS RolAdi,
            --        COUNT(*) AS Sayi
            -- FROM Kullanicilar
            -- GROUP BY Rol;

            """);

        return s.ToString().Replace("\r\n", "\n");
    }

    private static void Ekle(StringBuilder s, string tablo, string[] sutunlar, IEnumerable<string[]> satirlar)
    {
        var liste = satirlar.ToList();
        s.Append($"-- {tablo}: {liste.Count} kayıt\n");
        if (liste.Count > 0)
        {
            s.Append($"INSERT INTO `{tablo}` ({string.Join(", ", sutunlar.Select(c => $"`{c}`"))}) VALUES\n");
            s.Append(string.Join(",\n", liste.Select(d => "  (" + string.Join(", ", d) + ")")));
            s.Append(";\n");
        }
        s.Append('\n');
    }

    // MySQL metin kaçışları: \ ' ve satır sonları
    private static string Metin(string? v) => v is null ? "NULL"
        : "'" + v.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\0", "\\0")
                 .Replace("\r", "\\r").Replace("\n", "\\n").Replace("\x1a", "\\Z") + "'";

    private static string Sayi(int v) => v.ToString(Inv);
    private static string Para(decimal v) => v.ToString("0.00", Inv);
    private static string Tarih(DateTime v) => "'" + v.ToString("yyyy-MM-dd HH:mm:ss.ffffff", Inv) + "'";
}
