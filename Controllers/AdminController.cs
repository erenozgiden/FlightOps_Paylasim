using System.Text;
using FlightOps.Data;
using FlightOps.Models;
using FlightOps.Services;
using FlightOps.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightOps.Controllers;

// Admin paneli: personel yönetimi, kullanıcılar, biletler, raporlama ve veritabanı dışa aktarma
[Authorize(Roles = nameof(Rol.Admin))]
public class AdminController : Controller
{
    private readonly FlightOpsDbContext _db;
    private readonly FotoServisi _foto;

    public AdminController(FlightOpsDbContext db, FotoServisi foto)
    {
        _db = db;
        _foto = foto;
    }

    // ---------------- Personel ----------------

    public async Task<IActionResult> Personeller(string? ara)
    {
        var sorgu = _db.Kullanicilar.Where(k => k.Rol == Rol.Personel);
        if (!string.IsNullOrWhiteSpace(ara))
            sorgu = sorgu.Where(k => k.AdSoyad.Contains(ara) || k.KullaniciAdi.Contains(ara) || (k.Gorev != null && k.Gorev.Contains(ara)));

        ViewBag.Ara = ara;
        return View(await sorgu.OrderBy(k => k.AdSoyad).ToListAsync());
    }

    [HttpGet]
    public IActionResult PersonelEkle() => View("PersonelForm", new PersonelFormModel());

    [HttpPost]
    public async Task<IActionResult> PersonelEkle(PersonelFormModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Sifre))
            ModelState.AddModelError(nameof(model.Sifre), "Yeni personel için şifre zorunludur.");
        await FormuDogrula(model);

        if (!ModelState.IsValid)
            return View("PersonelForm", model);

        var personel = new Kullanici
        {
            Rol = Rol.Personel,
            SifreHash = Sifre.Hashle(model.Sifre!)
        };
        FormdanAktar(model, personel);
        if (model.Foto != null)
            personel.FotoYolu = await _foto.KaydetAsync(model.Foto);

        _db.Kullanicilar.Add(personel);
        await _db.SaveChangesAsync();

        TempData["Mesaj"] = $"{personel.AdSoyad} eklendi.";
        return RedirectToAction(nameof(Personeller));
    }

    [HttpGet]
    public async Task<IActionResult> PersonelDuzenle(int id)
    {
        var p = await PersonelBul(id);
        if (p == null) return NotFound();

        return View("PersonelForm", new PersonelFormModel
        {
            Id = p.Id, AdSoyad = p.AdSoyad, KullaniciAdi = p.KullaniciAdi, Gorev = p.Gorev,
            Eposta = p.Eposta, Telefon = p.Telefon, MevcutFotoYolu = p.FotoYolu
        });
    }

    [HttpPost]
    public async Task<IActionResult> PersonelDuzenle(PersonelFormModel model)
    {
        var personel = await PersonelBul(model.Id);
        if (personel == null) return NotFound();

        await FormuDogrula(model);
        if (!ModelState.IsValid)
        {
            model.MevcutFotoYolu = personel.FotoYolu;
            return View("PersonelForm", model);
        }

        FormdanAktar(model, personel);
        if (!string.IsNullOrWhiteSpace(model.Sifre))
            personel.SifreHash = Sifre.Hashle(model.Sifre);

        if (model.Foto != null || model.FotoyuKaldir)
        {
            _foto.Sil(personel.FotoYolu);
            personel.FotoYolu = model.Foto != null ? await _foto.KaydetAsync(model.Foto) : null;
        }

        await _db.SaveChangesAsync();
        TempData["Mesaj"] = $"{personel.AdSoyad} güncellendi.";
        return RedirectToAction(nameof(Personeller));
    }

    [HttpPost]
    public async Task<IActionResult> PersonelSil(int id)
    {
        var personel = await PersonelBul(id);
        if (personel == null) return NotFound();

        _foto.Sil(personel.FotoYolu);
        _db.Kullanicilar.Remove(personel);
        await _db.SaveChangesAsync();

        TempData["Mesaj"] = $"{personel.AdSoyad} silindi.";
        return RedirectToAction(nameof(Personeller));
    }

    private Task<Kullanici?> PersonelBul(int id) =>
        _db.Kullanicilar.FirstOrDefaultAsync(k => k.Id == id && k.Rol == Rol.Personel);

    private async Task FormuDogrula(PersonelFormModel model)
    {
        if (await _db.Kullanicilar.AnyAsync(k => k.KullaniciAdi == model.KullaniciAdi && k.Id != model.Id))
            ModelState.AddModelError(nameof(model.KullaniciAdi), "Bu kullanıcı adı alınmış.");

        if (model.Foto != null && _foto.Dogrula(model.Foto) is { } hata)
            ModelState.AddModelError(nameof(model.Foto), hata);
    }

    private static void FormdanAktar(PersonelFormModel model, Kullanici personel)
    {
        personel.AdSoyad = model.AdSoyad.Trim();
        personel.KullaniciAdi = model.KullaniciAdi.Trim();
        personel.Gorev = model.Gorev?.Trim();
        personel.Eposta = model.Eposta?.Trim();
        personel.Telefon = model.Telefon?.Trim();
    }

    // ---------------- Kullanıcılar ----------------

    public async Task<IActionResult> Kullanicilar(string? ara)
    {
        var sorgu = _db.Kullanicilar.Where(k => k.Rol == Rol.Kullanici);
        if (!string.IsNullOrWhiteSpace(ara))
            sorgu = sorgu.Where(k => k.AdSoyad.Contains(ara) || k.KullaniciAdi.Contains(ara) || (k.Eposta != null && k.Eposta.Contains(ara)));

        ViewBag.Ara = ara;
        return View(await sorgu
            .OrderBy(k => k.AdSoyad)
            .Select(k => new KullaniciSatiri
            {
                Kullanici = k,
                AktifBilet = k.Biletler.Count(b => b.Durum == BiletDurumu.Aktif)
            })
            .ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> KullaniciSil(int id)
    {
        var kullanici = await _db.Kullanicilar.FirstOrDefaultAsync(k => k.Id == id && k.Rol == Rol.Kullanici);
        if (kullanici == null) return NotFound();

        _foto.Sil(kullanici.FotoYolu);
        _db.Kullanicilar.Remove(kullanici); // biletleri de silinir (cascade)
        await _db.SaveChangesAsync();

        TempData["Mesaj"] = $"{kullanici.AdSoyad} ve biletleri silindi.";
        return RedirectToAction(nameof(Kullanicilar));
    }

    // ---------------- Veritabanı dışa aktarma (.sql) ----------------

    // Güncel veritabanını MySQL'e aktarılabilen tek bir .sql dosyası olarak indirir
    public async Task<IActionResult> VeritabaniIndir()
    {
        var sql = await MySqlDisaAktarici.UretAsync(_db, "Uygulamadaki güncel veriler");
        return File(new UTF8Encoding(false).GetBytes(sql), "application/sql", $"flightops_mysql_{DateTime.Now:yyyyMMdd_HHmm}.sql");
    }

    // ---------------- Biletler ----------------

    public async Task<IActionResult> Biletler(string? ara, BiletDurumu? durum)
    {
        var sorgu = _db.Biletler.Include(b => b.Ucus).Include(b => b.Kullanici).AsQueryable();

        if (!string.IsNullOrWhiteSpace(ara))
            sorgu = sorgu.Where(b => b.Ucus!.UcusNo.Contains(ara) || b.Kullanici!.AdSoyad.Contains(ara));
        if (durum != null)
            sorgu = sorgu.Where(b => b.Durum == durum);

        ViewBag.Ara = ara;
        ViewBag.Durum = durum;
        return View(await sorgu.OrderByDescending(b => b.SatinAlmaTarihi).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> BiletIptal(int id)
    {
        var bilet = await _db.Biletler.FindAsync(id);
        if (bilet == null) return NotFound();

        bilet.Durum = BiletDurumu.Iptal;
        await _db.SaveChangesAsync();
        TempData["Mesaj"] = $"#{bilet.Id} numaralı bilet iptal edildi.";
        return RedirectToAction(nameof(Biletler));
    }

    // ---------------- Raporlama ----------------

    public async Task<IActionResult> Raporlar()
    {
        var ucuslar = await _db.Ucuslar.Include(u => u.Biletler).ToListAsync();
        var biletler = await _db.Biletler.Include(b => b.Kullanici).ToListAsync();
        var aktif = biletler.Where(b => b.Durum == BiletDurumu.Aktif).ToList();

        var model = new RaporModel
        {
            ToplamUcus = ucuslar.Count,
            ToplamPersonel = await _db.Kullanicilar.CountAsync(k => k.Rol == Rol.Personel),
            ToplamKullanici = await _db.Kullanicilar.CountAsync(k => k.Rol == Rol.Kullanici),
            AktifBilet = aktif.Count,
            IptalBilet = biletler.Count - aktif.Count,
            ToplamGelir = aktif.Sum(b => b.Fiyat),

            DurumDagilimi = Enum.GetValues<UcusDurumu>()
                .Select(d => (d, ucuslar.Count(u => u.Durum == d))).ToList(),

            UcusBazinda = ucuslar.OrderBy(u => u.KalkisZamani).Select(u =>
            {
                var satilan = u.Biletler.Where(b => b.Durum == BiletDurumu.Aktif).ToList();
                return new UcusRaporSatiri
                {
                    UcusNo = u.UcusNo, Guzergah = $"{u.Kalkis} → {u.Varis}", KalkisZamani = u.KalkisZamani,
                    Durum = u.Durum, Kapasite = u.Kapasite, SatilanBilet = satilan.Count, Gelir = satilan.Sum(b => b.Fiyat)
                };
            }).ToList(),

            EnCokBiletAlanlar = aktif.GroupBy(b => b.Kullanici!.AdSoyad)
                .Select(g => (g.Key, g.Count(), g.Sum(b => b.Fiyat)))
                .OrderByDescending(x => x.Item2).Take(5).ToList(),

            SonYediGun = Enumerable.Range(0, 7).Select(i => DateTime.Today.AddDays(-6 + i)).Select(gun =>
            {
                var gunun = aktif.Where(b => b.SatinAlmaTarihi.Date == gun).ToList();
                return (gun, gunun.Count, gunun.Sum(b => b.Fiyat));
            }).ToList()
        };
        model.OrtalamaDoluluk = model.UcusBazinda.Count == 0 ? 0 : model.UcusBazinda.Average(u => u.Doluluk);

        return View(model);
    }
}
