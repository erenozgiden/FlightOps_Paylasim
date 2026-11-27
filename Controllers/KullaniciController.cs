using FlightOps.Data;
using FlightOps.Models;
using FlightOps.Services;
using FlightOps.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightOps.Controllers;

// Kullanıcı paneli: uçuşları görüntüleme/filtreleme, bilet, profil ve şifre
[Authorize(Roles = nameof(Rol.Kullanici))]
[TypeFilter(typeof(ProfilZorunluFiltresi))] // fotoğraf ve adres eklenmeden panel kullanılamaz
public class KullaniciController : Controller
{
    private readonly FlightOpsDbContext _db;
    private readonly FotoServisi _foto;

    public KullaniciController(FlightOpsDbContext db, FotoServisi foto)
    {
        _db = db;
        _foto = foto;
    }

    private int Ben => User.KullaniciId();

    public async Task<IActionResult> Index(UcusFiltreModel filtre)
    {
        var sorgu = _db.Ucuslar.Include(u => u.Biletler).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtre.Kalkis))
            sorgu = sorgu.Where(u => u.Kalkis.Contains(filtre.Kalkis));
        if (!string.IsNullOrWhiteSpace(filtre.Varis))
            sorgu = sorgu.Where(u => u.Varis.Contains(filtre.Varis));
        if (filtre.Tarih != null)
        {
            var gun = filtre.Tarih.Value.Date;
            sorgu = sorgu.Where(u => u.KalkisZamani >= gun && u.KalkisZamani < gun.AddDays(1));
        }
        if (filtre.Durum != null)
            sorgu = sorgu.Where(u => u.Durum == filtre.Durum);

        var ben = Ben;
        filtre.Ucuslar = (await sorgu.OrderBy(u => u.KalkisZamani).ToListAsync()).Select(u =>
        {
            var aktif = u.Biletler.Where(b => b.Durum == BiletDurumu.Aktif).ToList();
            return new UcusSatiri
            {
                Ucus = u,
                SatilanKoltuk = aktif.Count,
                KullanicininBileti = aktif.Any(b => b.KullaniciId == ben),
                BiletAlinabilir = aktif.Count < u.Kapasite
                    && u.Durum is UcusDurumu.Planlandi or UcusDurumu.Gecikmeli or UcusDurumu.Binis
            };
        }).ToList();

        // "Nereden / Nereye" listeleri ve harita için tüm uçuşlar
        var tumUcuslar = await _db.Ucuslar.AsNoTracking().OrderBy(u => u.KalkisZamani).ToListAsync();
        filtre.Sehirler = tumUcuslar
            .SelectMany(u => new[] { Havalimanlari.SehirAdi(u.Kalkis), Havalimanlari.SehirAdi(u.Varis) })
            .Distinct().OrderBy(s => s, StringComparer.Create(new System.Globalization.CultureInfo("tr-TR"), false)).ToList();

        var eslesenler = filtre.Ucuslar.Select(s => s.Ucus.Id).ToHashSet();
        filtre.Harita = new HaritaModel
        {
            Havalimanlari = Havalimanlari.Liste.Select(h => new HaritaHavalimani(h.Kod, h.Sehir, h.Ad, h.Enlem, h.Boylam, h.YurtDisi)).ToList(),
            Ucuslar = tumUcuslar.Select(u => new HaritaUcusu(
                u.Id, u.UcusNo,
                Havalimanlari.Bul(u.Kalkis)?.Kod, Havalimanlari.Bul(u.Varis)?.Kod,
                Havalimanlari.SehirAdi(u.Kalkis), Havalimanlari.SehirAdi(u.Varis),
                u.KalkisZamani.ToString("dd.MM HH:mm"), u.Durum.Ad(), u.Durum.DurumSinifi(),
                u.Fiyat.Tl(), eslesenler.Contains(u.Id))).ToList(),
            SeciliKalkis = filtre.Kalkis,
            SeciliVaris = filtre.Varis
        };

        return View(filtre);
    }

    // ---------------- Bilet al: kredi kartı ile ödeme ----------------

    [HttpGet]
    public async Task<IActionResult> Odeme(int id)
    {
        var ucus = await SatistakiUcus(id);
        if (ucus == null)
        {
            TempData["Hata"] = "Bu uçuş için bilet alınamıyor.";
            return RedirectToAction(nameof(Index));
        }

        var model = new OdemeModel { UcusId = id, SonKullanmaAy = 0, SonKullanmaYil = 0 };
        return View(OzetiDoldur(model, ucus));
    }

    [HttpPost]
    public async Task<IActionResult> Odeme(OdemeModel model)
    {
        var ucus = await SatistakiUcus(model.UcusId);
        if (ucus == null)
        {
            TempData["Hata"] = "Bu uçuş için bilet alınamıyor.";
            return RedirectToAction(nameof(Index));
        }

        var rakamlar = KrediKarti.Temizle(model.KartNumarasi);
        if (!string.IsNullOrEmpty(model.KartNumarasi) && !KrediKarti.LuhnGecerli(rakamlar))
            ModelState.AddModelError(nameof(model.KartNumarasi), "Kart numarası geçersiz.");

        if (model.SonKullanmaAy is >= 1 and <= 12 && model.SonKullanmaYil >= 2000
            && KrediKarti.SuresiGecmis(model.SonKullanmaAy, model.SonKullanmaYil))
            ModelState.AddModelError(nameof(model.SonKullanmaYil), "Kartın son kullanma tarihi geçmiş.");

        if (!ModelState.IsValid)
        {
            model.Cvv = "";
            return View(OzetiDoldur(model, ucus));
        }

        // İlk boş koltuğu ver
        var dolu = ucus.Biletler.Where(b => b.Durum == BiletDurumu.Aktif).Select(b => b.KoltukNo).ToHashSet();
        int koltuk = Enumerable.Range(1, ucus.Kapasite).First(k => !dolu.Contains(k));

        // Kart numarasının tamamı ve CVV kaydedilmez
        _db.Biletler.Add(new Bilet
        {
            UcusId = ucus.Id,
            KullaniciId = Ben,
            KoltukNo = koltuk,
            Fiyat = ucus.Fiyat,
            KartSahibi = model.KartSahibi.Trim().ToUpper(),
            KartTipi = KrediKarti.Tip(rakamlar),
            KartSonDortHane = rakamlar[^4..]
        });
        await _db.SaveChangesAsync();

        TempData["Mesaj"] = $"Ödeme alındı. {ucus.UcusNo} için biletiniz oluşturuldu. Koltuk: {koltuk}";
        return RedirectToAction(nameof(Biletlerim));
    }

    // Satışta olan (dolmamış, iptal/kalkmış olmayan) uçuşu getirir
    private async Task<Ucus?> SatistakiUcus(int id)
    {
        var ucus = await _db.Ucuslar.Include(u => u.Biletler).FirstOrDefaultAsync(u => u.Id == id);
        if (ucus == null) return null;

        bool satistaMi = ucus.Durum is UcusDurumu.Planlandi or UcusDurumu.Gecikmeli or UcusDurumu.Binis;
        int satilan = ucus.Biletler.Count(b => b.Durum == BiletDurumu.Aktif);
        return satistaMi && satilan < ucus.Kapasite ? ucus : null;
    }

    private static OdemeModel OzetiDoldur(OdemeModel model, Ucus ucus)
    {
        model.Ucus = ucus;
        model.BosKoltuk = ucus.Kapasite - ucus.Biletler.Count(b => b.Durum == BiletDurumu.Aktif);
        return model;
    }

    public async Task<IActionResult> Biletlerim()
    {
        var biletler = await _db.Biletler.Include(b => b.Ucus)
            .Where(b => b.KullaniciId == Ben)
            .OrderByDescending(b => b.SatinAlmaTarihi)
            .ToListAsync();
        return View(biletler);
    }

    [HttpPost]
    public async Task<IActionResult> BiletIptal(int id)
    {
        var bilet = await _db.Biletler.Include(b => b.Ucus)
            .FirstOrDefaultAsync(b => b.Id == id && b.KullaniciId == Ben);
        if (bilet == null) return NotFound();

        if (bilet.Ucus!.Durum is UcusDurumu.Kalkti or UcusDurumu.Indi)
        {
            TempData["Hata"] = "Kalkmış bir uçuşun bileti iptal edilemez.";
            return RedirectToAction(nameof(Biletlerim));
        }

        bilet.Durum = BiletDurumu.Iptal;
        await _db.SaveChangesAsync();
        TempData["Mesaj"] = "Biletiniz iptal edildi.";
        return RedirectToAction(nameof(Biletlerim));
    }

    [HttpGet]
    public async Task<IActionResult> Profil()
    {
        var k = await _db.Kullanicilar.FindAsync(Ben);
        if (k == null) return NotFound();

        return View(new ProfilModel
        {
            KullaniciAdi = k.KullaniciAdi, AdSoyad = k.AdSoyad, Eposta = k.Eposta, Telefon = k.Telefon,
            Adres = k.Adres, MevcutFotoYolu = k.FotoYolu
        });
    }

    [HttpPost]
    public async Task<IActionResult> Profil(ProfilModel model)
    {
        var k = await _db.Kullanicilar.FindAsync(Ben);
        if (k == null) return NotFound();

        model.KullaniciAdi = k.KullaniciAdi;
        model.MevcutFotoYolu = k.FotoYolu;

        // Fotoğrafı yoksa yüklemek zorunlu
        if (model.Foto == null && string.IsNullOrEmpty(k.FotoYolu))
            ModelState.AddModelError(nameof(model.Foto), "Profil fotoğrafı zorunludur.");
        if (model.Foto != null && _foto.Dogrula(model.Foto) is { } fotoHatasi)
            ModelState.AddModelError(nameof(model.Foto), fotoHatasi);

        if (!ModelState.IsValid)
            return View(model);

        bool ilkTamamlama = k.ProfilEksik;

        k.AdSoyad = model.AdSoyad.Trim();
        k.Eposta = model.Eposta?.Trim();
        k.Telefon = model.Telefon?.Trim();
        k.Adres = model.Adres!.Trim();
        if (model.Foto != null)
        {
            _foto.Sil(k.FotoYolu);
            k.FotoYolu = await _foto.KaydetAsync(model.Foto);
        }
        await _db.SaveChangesAsync();

        // Üst bilgideki ad ve fotoğraf güncellensin
        await Oturum.Ac(HttpContext, k);

        if (ilkTamamlama)
        {
            TempData["Mesaj"] = "Profiliniz tamamlandı. Artık uçuşları görebilirsiniz.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Mesaj"] = "Profil bilgileriniz güncellendi.";
        return RedirectToAction(nameof(Profil));
    }

    [HttpGet]
    public IActionResult SifreDegistir() => View(new SifreDegistirModel());

    [HttpPost]
    public async Task<IActionResult> SifreDegistir(SifreDegistirModel model)
    {
        var k = await _db.Kullanicilar.FindAsync(Ben);
        if (k == null) return NotFound();

        if (!string.IsNullOrEmpty(model.MevcutSifre) && !Sifre.Dogrula(k.SifreHash, model.MevcutSifre))
            ModelState.AddModelError(nameof(model.MevcutSifre), "Mevcut şifre yanlış.");

        if (!ModelState.IsValid)
            return View(new SifreDegistirModel());

        k.SifreHash = Sifre.Hashle(model.YeniSifre);
        await _db.SaveChangesAsync();

        TempData["Mesaj"] = "Şifreniz değiştirildi.";
        return RedirectToAction(nameof(SifreDegistir));
    }
}
