using FlightOps.Data;
using FlightOps.Models;
using FlightOps.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightOps.Controllers;

// Personel paneli: uçuş ekleme, silme, güncelleme (admin de kullanabilir)
[Authorize(Roles = nameof(Rol.Personel) + "," + nameof(Rol.Admin))]
public class PersonelController : Controller
{
    private readonly FlightOpsDbContext _db;

    public PersonelController(FlightOpsDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? ara)
    {
        var sorgu = _db.Ucuslar.Include(u => u.Biletler).AsQueryable();
        if (!string.IsNullOrWhiteSpace(ara))
            sorgu = sorgu.Where(u => u.UcusNo.Contains(ara) || u.Kalkis.Contains(ara) || u.Varis.Contains(ara) || (u.Kapi != null && u.Kapi.Contains(ara)));

        ViewBag.Ara = ara;
        return View(await sorgu.OrderBy(u => u.KalkisZamani).ToListAsync());
    }

    [HttpGet]
    public IActionResult UcusEkle() => View("UcusForm", new Ucus());

    [HttpPost]
    public async Task<IActionResult> UcusEkle(Ucus model)
    {
        await Dogrula(model);
        if (!ModelState.IsValid)
            return View("UcusForm", model);

        model.UcusNo = model.UcusNo.Trim().ToUpperInvariant();
        _db.Ucuslar.Add(model);
        await _db.SaveChangesAsync();

        TempData["Mesaj"] = $"{model.UcusNo} uçuşu eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> UcusDuzenle(int id)
    {
        var ucus = await _db.Ucuslar.FindAsync(id);
        return ucus == null ? NotFound() : View("UcusForm", ucus);
    }

    [HttpPost]
    public async Task<IActionResult> UcusDuzenle(Ucus model)
    {
        var ucus = await _db.Ucuslar.FindAsync(model.Id);
        if (ucus == null) return NotFound();

        await Dogrula(model);
        if (!ModelState.IsValid)
            return View("UcusForm", model);

        ucus.UcusNo = model.UcusNo.Trim().ToUpperInvariant();
        ucus.Kalkis = model.Kalkis.Trim();
        ucus.Varis = model.Varis.Trim();
        ucus.KalkisZamani = model.KalkisZamani;
        ucus.VarisZamani = model.VarisZamani;
        ucus.Kapi = model.Kapi?.Trim();
        ucus.Kapasite = model.Kapasite;
        ucus.Fiyat = model.Fiyat;
        ucus.Durum = model.Durum;
        await _db.SaveChangesAsync();

        TempData["Mesaj"] = $"{ucus.UcusNo} uçuşu güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DurumGuncelle(int id, UcusDurumu durum)
    {
        var ucus = await _db.Ucuslar.FindAsync(id);
        if (ucus == null) return NotFound();

        ucus.Durum = durum;
        await _db.SaveChangesAsync();
        TempData["Mesaj"] = $"{ucus.UcusNo} durumu: {durum.Ad()}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UcusSil(int id)
    {
        var ucus = await _db.Ucuslar.FindAsync(id);
        if (ucus == null) return NotFound();

        _db.Ucuslar.Remove(ucus);
        await _db.SaveChangesAsync();

        TempData["Mesaj"] = $"{ucus.UcusNo} uçuşu silindi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task Dogrula(Ucus model)
    {
        var no = model.UcusNo?.Trim().ToUpperInvariant();
        if (await _db.Ucuslar.AnyAsync(u => u.UcusNo == no && u.Id != model.Id))
            ModelState.AddModelError(nameof(model.UcusNo), "Bu uçuş numarası zaten var.");

        if (model.VarisZamani <= model.KalkisZamani)
            ModelState.AddModelError(nameof(model.VarisZamani), "Varış zamanı kalkıştan sonra olmalı.");

        if (model.Id != 0)
        {
            var satilan = await _db.Biletler.CountAsync(b => b.UcusId == model.Id && b.Durum == BiletDurumu.Aktif);
            if (model.Kapasite < satilan)
                ModelState.AddModelError(nameof(model.Kapasite), $"Bu uçuşta {satilan} bilet satılmış; kapasite daha az olamaz.");
        }
    }
}
