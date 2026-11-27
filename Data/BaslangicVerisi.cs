using FlightOps.Models;
using FlightOps.Services;
using Microsoft.EntityFrameworkCore;

namespace FlightOps.Data;

// Veritabanı ilk oluşturulduğunda örnek kayıtları ekler
public static class BaslangicVerisi
{
    public static void Ekle(FlightOpsDbContext db)
    {
        bool yeniOlusturuldu = db.Database.EnsureCreated();

        if (db.Database.IsSqlite())
            SemayiGuncelle(db);
        else if (yeniOlusturuldu && db.Database.IsMySql())
            // MySQL veritabanını uygulama kendisi kurduysa varsayılan karakter ayarı da Türkçe olsun
            db.Database.ExecuteSqlRaw("ALTER DATABASE CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci");

        if (db.Kullanicilar.Any())
            return;

        var admin = new Kullanici
        {
            KullaniciAdi = "admin", AdSoyad = "Sistem Yöneticisi", Rol = Rol.Admin,
            Eposta = "admin@flightops.local", SifreHash = Sifre.Hashle("1234")
        };
        var personeller = new[]
        {
            new Kullanici { KullaniciAdi = "personel", AdSoyad = "Ayşe Yılmaz", Rol = Rol.Personel, Gorev = "Operasyon Sorumlusu", Telefon = "0532 000 00 01", SifreHash = Sifre.Hashle("1234") },
            new Kullanici { KullaniciAdi = "mkaya", AdSoyad = "Mehmet Kaya", Rol = Rol.Personel, Gorev = "Pilot", Telefon = "0532 000 00 02", SifreHash = Sifre.Hashle("1234") },
        };
        var kullanicilar = new[]
        {
            new Kullanici { KullaniciAdi = "kullanici", AdSoyad = "Can Demir", Rol = Rol.Kullanici, Eposta = "can@ornek.com", SifreHash = Sifre.Hashle("1234") },
            new Kullanici { KullaniciAdi = "zeynep", AdSoyad = "Zeynep Arslan", Rol = Rol.Kullanici, Eposta = "zeynep@ornek.com", SifreHash = Sifre.Hashle("1234") },
        };
        db.Kullanicilar.Add(admin);
        db.Kullanicilar.AddRange(personeller);
        db.Kullanicilar.AddRange(kullanicilar);

        var bugun = DateTime.Today;
        var ucuslar = new[]
        {
            new Ucus { UcusNo = "TK101", Kalkis = "İstanbul (IST)", Varis = "Ankara (ESB)", KalkisZamani = bugun.AddHours(9), VarisZamani = bugun.AddHours(10).AddMinutes(10), Kapi = "A3", Kapasite = 180, Fiyat = 1450, Durum = UcusDurumu.Kalkti },
            new Ucus { UcusNo = "PC202", Kalkis = "İstanbul (SAW)", Varis = "İzmir (ADB)", KalkisZamani = bugun.AddHours(13), VarisZamani = bugun.AddHours(14).AddMinutes(5), Kapi = "B7", Kapasite = 189, Fiyat = 1200, Durum = UcusDurumu.Binis },
            new Ucus { UcusNo = "TK305", Kalkis = "Ankara (ESB)", Varis = "Antalya (AYT)", KalkisZamani = bugun.AddDays(1).AddHours(8), VarisZamani = bugun.AddDays(1).AddHours(9).AddMinutes(15), Kapi = "C1", Kapasite = 160, Fiyat = 1650, Durum = UcusDurumu.Planlandi },
            new Ucus { UcusNo = "AJ410", Kalkis = "İzmir (ADB)", Varis = "Trabzon (TZX)", KalkisZamani = bugun.AddDays(1).AddHours(16), VarisZamani = bugun.AddDays(1).AddHours(18), Kapi = "A9", Kapasite = 150, Fiyat = 1900, Durum = UcusDurumu.Gecikmeli },
            new Ucus { UcusNo = "TK520", Kalkis = "İstanbul (IST)", Varis = "Londra (LHR)", KalkisZamani = bugun.AddDays(2).AddHours(11), VarisZamani = bugun.AddDays(2).AddHours(13).AddMinutes(50), Kapi = "F12", Kapasite = 300, Fiyat = 6200, Durum = UcusDurumu.Planlandi },
        };
        db.Ucuslar.AddRange(ucuslar);
        db.SaveChanges();

        // Raporlarda veri görünsün diye birkaç örnek bilet
        int koltuk = 1;
        foreach (var (ucus, kullanici) in new[]
        {
            (ucuslar[0], kullanicilar[0]), (ucuslar[1], kullanicilar[0]), (ucuslar[1], kullanicilar[1]),
            (ucuslar[2], kullanicilar[1]), (ucuslar[4], kullanicilar[0]),
        })
        {
            db.Biletler.Add(new Bilet
            {
                UcusId = ucus.Id, KullaniciId = kullanici.Id, KoltukNo = koltuk++,
                Fiyat = ucus.Fiyat, SatinAlmaTarihi = bugun.AddDays(-koltuk)
            });
        }
        db.SaveChanges();
    }

    // Eski sürümle oluşturulmuş SQLite veritabanına yeni sütunları ekler (kayıtlar silinmez).
    // MySQL'de tablolar zaten güncel .sql dosyasından ya da EnsureCreated ile tam oluşturulur.
    private static void SemayiGuncelle(FlightOpsDbContext db)
    {
        var mevcut = db.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('Biletler')")
            .ToList();

        // Sütun adları sabit listeden gelir, kullanıcı girdisi değildir
        string[] yeniSutunlar = { "KartSahibi", "KartTipi", "KartSonDortHane" };
        foreach (var sutun in yeniSutunlar.Where(s => !mevcut.Contains(s)))
            db.Database.ExecuteSqlRaw("ALTER TABLE Biletler ADD COLUMN " + sutun + " TEXT NULL");

        var kullaniciSutunlari = db.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('Kullanicilar')")
            .ToList();
        if (!kullaniciSutunlari.Contains("Adres"))
            db.Database.ExecuteSqlRaw("ALTER TABLE Kullanicilar ADD COLUMN Adres TEXT NULL");
    }
}
