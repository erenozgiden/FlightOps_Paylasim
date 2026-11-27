using System.Security.Claims;
using FlightOps.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FlightOps.Services;

// Giriş çerezini oluşturur. Profil (ad, fotoğraf) değişince de yeniden çağrılır,
// böylece üst bilgideki ad ve fotoğraf güncel kalır.
public static class Oturum
{
    public static Task Ac(HttpContext http, Kullanici kullanici)
    {
        var bilgiler = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
            new(ClaimTypes.Name, kullanici.KullaniciAdi),
            new("AdSoyad", kullanici.AdSoyad),
            new(ClaimTypes.Role, kullanici.Rol.ToString())
        };
        if (!string.IsNullOrEmpty(kullanici.FotoYolu))
            bilgiler.Add(new Claim("Foto", kullanici.FotoYolu));

        var kimlik = new ClaimsIdentity(bilgiler, CookieAuthenticationDefaults.AuthenticationScheme);
        return http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(kimlik));
    }
}
