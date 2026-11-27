using FlightOps.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FlightOps.Data;

// Uygulamanın veritabanı bağlamı. Aynı model hem SQLite hem MySQL ile çalışır;
// hangisinin kullanılacağı appsettings.json dosyasındaki "Veritabani:Tur" ayarından seçilir.
public class FlightOpsDbContext : DbContext
{
    public FlightOpsDbContext(DbContextOptions<FlightOpsDbContext> options) : base(options) { }

    public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();
    public DbSet<Ucus> Ucuslar => Set<Ucus>();
    public DbSet<Bilet> Biletler => Set<Bilet>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Kullanici>().HasIndex(k => k.KullaniciAdi).IsUnique();
        model.Entity<Ucus>().HasIndex(u => u.UcusNo).IsUnique();

        model.Entity<Bilet>()
            .HasOne(b => b.Ucus).WithMany(u => u.Biletler)
            .HasForeignKey(b => b.UcusId).OnDelete(DeleteBehavior.Cascade);

        model.Entity<Bilet>()
            .HasOne(b => b.Kullanici).WithMany(k => k.Biletler)
            .HasForeignKey(b => b.KullaniciId).OnDelete(DeleteBehavior.Cascade);

        if (Database.IsMySql())
            MySqlAyarlari(model);
        else
        {
            // SQLite decimal türünü doğrudan desteklemediği için fiyatlar sayı olarak saklanır
            model.Entity<Ucus>().Property(u => u.Fiyat).HasConversion<double>();
            model.Entity<Bilet>().Property(b => b.Fiyat).HasConversion<double>();
        }
    }

    // MySQL'e özel ayarlar: Türkçe karakter seti, para sütunları, açıklamalar ve kontrol kısıtları
    private static void MySqlAyarlari(ModelBuilder model)
    {
        model.HasCharSet("utf8mb4");
        model.UseCollation("utf8mb4_turkish_ci");

        model.Entity<Kullanici>(e =>
        {
            e.ToTable(t =>
            {
                t.HasComment("Admin, personel ve kullanıcılar (Rol sütunu ile ayrılır)");
                t.HasCheckConstraint("CK_Kullanicilar_Rol", "`Rol` BETWEEN 0 AND 2");
            });
            e.Property(k => k.SifreHash).HasMaxLength(255).HasComment("Şifre özeti (hash), düz metin değil");
            e.Property(k => k.Eposta).HasComment("Şifremi unuttum doğrulaması için");
            e.Property(k => k.Rol).HasComment("0 = Admin, 1 = Personel, 2 = Kullanici");
            e.Property(k => k.Gorev).HasComment("Personelin görevi (Pilot, Kabin Memuru ...)");
            e.Property(k => k.FotoYolu).HasMaxLength(255).HasComment("Profil fotoğrafının dosya yolu");
            e.Property(k => k.Adres).HasComment("Ev adresi (kullanıcılar için zorunlu)");
        });

        model.Entity<Ucus>(e =>
        {
            e.ToTable(t =>
            {
                t.HasComment("Uçuşlar");
                t.HasCheckConstraint("CK_Ucuslar_Zaman", "`VarisZamani` > `KalkisZamani`");
                t.HasCheckConstraint("CK_Ucuslar_Kapasite", "`Kapasite` BETWEEN 1 AND 600");
                t.HasCheckConstraint("CK_Ucuslar_Durum", "`Durum` BETWEEN 0 AND 5");
            });
            e.Property(u => u.Kalkis).HasComment("Örn. İstanbul (IST)");
            e.Property(u => u.Varis).HasComment("Örn. Ankara (ESB)");
            e.Property(u => u.Fiyat).HasPrecision(10, 2).HasComment("Bilet fiyatı (TL)");
            e.Property(u => u.Durum).HasComment("0 = Planlandı, 1 = Biniş, 2 = Kalktı, 3 = İndi, 4 = Gecikmeli, 5 = İptal");
        });

        model.Entity<Bilet>(e =>
        {
            e.ToTable(t =>
            {
                t.HasComment("Satılan biletler (Ucuslar ve Kullanicilar tablolarına bağlı)");
                t.HasCheckConstraint("CK_Biletler_Durum", "`Durum` IN (0, 1)");
            });
            e.Property(b => b.Fiyat).HasPrecision(10, 2).HasComment("Satın alındığı andaki fiyat (TL)");
            e.Property(b => b.Durum).HasComment("0 = Aktif, 1 = İptal");
            e.Property(b => b.KartSahibi).HasMaxLength(60);
            e.Property(b => b.KartTipi).HasMaxLength(20).HasComment("Visa, Mastercard, Troy, Amex");
            e.Property(b => b.KartSonDortHane).HasMaxLength(4).IsFixedLength().HasComment("Kartın yalnızca son 4 hanesi (tam numara ve CVV saklanmaz)");
        });
    }
}

// Entity Framework modeli önbelleğe alırken veritabanı türünü de hesaba katsın
// (aynı programda hem SQLite hem MySQL modeli kullanılabildiği için gerekli)
public class SaglayiciyaGoreModelAnahtari : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        (context.GetType(), context.Database.ProviderName, designTime);
}
