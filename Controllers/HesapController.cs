using FlightOps.Data;
using FlightOps.Models;
using FlightOps.Services;
using FlightOps.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightOps.Controllers;

// Giriş, kayıt ve çıkış işlemleri
public class HesapController : Controller
{
    private readonly FlightOpsDbContext _db;
    private readonly FotoServisi _foto;

    public HesapController(FlightOpsDbContext db, FotoServisi foto)
    {
        _db = db;
        _foto = foto;
    }

    // Ana adres: giriş yapılmışsa panele, yapılmamışsa giriş ekranına
    public IActionResult Index() =>
        User.Identity?.IsAuthenticated == true ? RedirectToAction(nameof(Panel)) : RedirectToAction(nameof(Giris));

    // Giriş yapan kişiyi rolüne göre kendi paneline yönlendirir
    [Authorize]
    public IActionResult Panel()
    {
        if (User.IsInRole(nameof(Rol.Admin))) return RedirectToAction("Personeller", "Admin");
        if (User.IsInRole(nameof(Rol.Personel))) return RedirectToAction("Index", "Personel");
        return RedirectToAction("Index", "Kullanici");
    }

    [HttpGet]
    public IActionResult Giris(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction(nameof(Panel));

        return View(new GirisModel { DonusAdresi = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Giris(GirisModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var kullanici = await _db.Kullanicilar
            .FirstOrDefaultAsync(k => k.KullaniciAdi == model.KullaniciAdi);

        if (kullanici == null || !Sifre.Dogrula(kullanici.SifreHash, model.Sifre))
        {
            // Eski uygulamadaki gibi: uyarı göster, şifre alanını temizle
            ViewBag.Hata = "Kullanıcı adı veya şifre yanlış.";
            ModelState.Remove(nameof(GirisModel.Sifre));
            model.Sifre = "";
            return View(model);
        }

        await OturumAc(kullanici);

        if (!string.IsNullOrEmpty(model.DonusAdresi) && Url.IsLocalUrl(model.DonusAdresi))
            return LocalRedirect(model.DonusAdresi);

        return RedirectToAction(nameof(Panel));
    }

    [HttpGet]
    public IActionResult Kayit() => View(new KayitModel());

    [HttpPost]
    public async Task<IActionResult> Kayit(KayitModel model)
    {
        if (await _db.Kullanicilar.AnyAsync(k => k.KullaniciAdi == model.KullaniciAdi))
            ModelState.AddModelError(nameof(model.KullaniciAdi), "Bu kullanıcı adı alınmış.");

        if (model.Foto != null && _foto.Dogrula(model.Foto) is { } fotoHatasi)
            ModelState.AddModelError(nameof(model.Foto), fotoHatasi);

        if (!ModelState.IsValid)
            return View(model);

        var kullanici = new Kullanici
        {
            AdSoyad = model.AdSoyad.Trim(),
            KullaniciAdi = model.KullaniciAdi.Trim(),
            Eposta = model.Eposta,
            Adres = model.Adres.Trim(),
            FotoYolu = await _foto.KaydetAsync(model.Foto!),
            Rol = Rol.Kullanici,
            SifreHash = Sifre.Hashle(model.Sifre)
        };
        _db.Kullanicilar.Add(kullanici);
        await _db.SaveChangesAsync();

        await OturumAc(kullanici);
        return RedirectToAction(nameof(Panel));
    }

    // ---------------- Şifremi unuttum ----------------
    // Kullanıcı adı ve kayıtlı e-posta eşleşirse yeni şifre belirlenir

    [HttpGet]
    public IActionResult SifremiUnuttum() => View(new SifremiUnuttumModel());

    [HttpPost]
    public async Task<IActionResult> SifremiUnuttum(SifremiUnuttumModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var kullanici = await _db.Kullanicilar
            .FirstOrDefaultAsync(k => k.KullaniciAdi == model.KullaniciAdi.Trim());

        bool eslesti = kullanici?.Eposta != null
            && string.Equals(kullanici.Eposta.Trim(), model.Eposta.Trim(), StringComparison.OrdinalIgnoreCase);

        if (!eslesti)
        {
            // Hangi bilginin yanlış olduğu söylenmez (güvenlik)
            ModelState.AddModelError("", "Kullanıcı adı ile kayıtlı e-posta eşleşmedi. E-postanız kayıtlı değilse yöneticiye başvurun.");
            return View(model);
        }

        kullanici!.SifreHash = Sifre.Hashle(model.YeniSifre);
        await _db.SaveChangesAsync();

        TempData["Bilgi"] = "Şifreniz yenilendi. Yeni şifrenizle giriş yapabilirsiniz.";
        return RedirectToAction(nameof(Giris));
    }

    [HttpPost]
    public async Task<IActionResult> Cikis()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Giris));
    }

    public IActionResult ErisimEngellendi() => View();

    private Task OturumAc(Kullanici kullanici) => Oturum.Ac(HttpContext, kullanici);
}
