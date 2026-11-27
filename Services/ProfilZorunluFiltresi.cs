using FlightOps.Data;
using FlightOps.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FlightOps.Services;

// Kullanıcı panelinde profil fotoğrafı veya ev adresi eksikse,
// bunları ekleyene kadar her sayfa Profil sayfasına yönlendirilir.
public class ProfilZorunluFiltresi : IAsyncActionFilter
{
    private readonly FlightOpsDbContext _db;

    public ProfilZorunluFiltresi(FlightOpsDbContext db) => _db = db;

    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate devam)
    {
        var action = ctx.RouteData.Values["action"]?.ToString();
        bool serbest = action is "Profil" or "SifreDegistir";

        if (!serbest && ctx.HttpContext.User.IsInRole(nameof(Rol.Kullanici)))
        {
            var kullanici = await _db.Kullanicilar.FindAsync(ctx.HttpContext.User.KullaniciId());
            if (kullanici is { ProfilEksik: true })
            {
                if (ctx.Controller is Controller c)
                    c.TempData["Hata"] = "Devam etmek için profil fotoğrafınızı ve ev adresinizi eklemelisiniz.";
                ctx.Result = new RedirectToActionResult("Profil", "Kullanici", null);
                return;
            }
        }

        await devam();
    }
}
