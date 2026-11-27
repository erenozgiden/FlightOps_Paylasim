using FlightOps.Models;
using Microsoft.AspNetCore.Identity;

namespace FlightOps.Services;

// Şifreler veritabanında düz metin değil, güvenli özet (hash) olarak saklanır
public static class Sifre
{
    private static readonly PasswordHasher<Kullanici> Hasher = new();

    public static string Hashle(string sifre) => Hasher.HashPassword(null!, sifre);

    public static bool Dogrula(string hash, string sifre) =>
        Hasher.VerifyHashedPassword(null!, hash, sifre) != PasswordVerificationResult.Failed;
}
