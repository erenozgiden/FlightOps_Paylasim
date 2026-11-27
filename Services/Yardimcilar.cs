using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Claims;
using FlightOps.Models;

namespace FlightOps.Services;

public static class Yardimcilar
{
    // Enum'un [Display(Name = ...)] değerini döner (örn. Planlandi -> "Planlandı")
    public static string Ad(this Enum deger) =>
        deger.GetType().GetMember(deger.ToString()).FirstOrDefault()?
            .GetCustomAttribute<DisplayAttribute>()?.GetName() ?? deger.ToString();

    public static string DurumSinifi(this UcusDurumu durum) => durum switch
    {
        UcusDurumu.Planlandi => "etiket-mavi",
        UcusDurumu.Binis => "etiket-yesil",
        UcusDurumu.Kalkti => "etiket-gri",
        UcusDurumu.Indi => "etiket-gri",
        UcusDurumu.Gecikmeli => "etiket-sari",
        UcusDurumu.Iptal => "etiket-kirmizi",
        _ => ""
    };

    public static int KullaniciId(this ClaimsPrincipal kullanici) =>
        int.Parse(kullanici.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static string Tl(this decimal tutar) => tutar.ToString("N0") + " ₺";
}
