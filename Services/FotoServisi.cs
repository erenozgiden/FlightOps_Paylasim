namespace FlightOps.Services;

// Personel fotoğraflarını kalıcı veri klasöründeki "yuklemeler" klasörüne kaydeder
public class FotoServisi
{
    private static readonly string[] IzinliUzantilar = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaksimumBoyut = 5 * 1024 * 1024; // 5 MB

    private readonly string _klasor;

    public FotoServisi(VeriKlasoru veri) => _klasor = veri.Yuklemeler;

    // Hata varsa mesajı döner, yoksa null
    public string? Dogrula(IFormFile dosya)
    {
        var uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();
        if (!IzinliUzantilar.Contains(uzanti))
            return "Fotoğraf JPG, PNG, WEBP veya GIF olmalı.";
        if (dosya.Length > MaksimumBoyut)
            return "Fotoğraf en fazla 5 MB olabilir.";
        return null;
    }

    public async Task<string> KaydetAsync(IFormFile dosya)
    {
        var ad = $"{Guid.NewGuid():N}{Path.GetExtension(dosya.FileName).ToLowerInvariant()}";
        await using var akis = File.Create(Path.Combine(_klasor, ad));
        await dosya.CopyToAsync(akis);
        return "/yuklemeler/" + ad;
    }

    public void Sil(string? yol)
    {
        if (string.IsNullOrEmpty(yol) || !yol.StartsWith("/yuklemeler/"))
            return;

        var tamYol = Path.Combine(_klasor, Path.GetFileName(yol));
        if (File.Exists(tamYol))
            File.Delete(tamYol);
    }
}
