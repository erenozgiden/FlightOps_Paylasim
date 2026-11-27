namespace FlightOps.Data;

public record Havalimani(string Kod, string Sehir, string Ad, double Enlem, double Boylam, bool YurtDisi = false)
{
    // Uçuş formlarında kullanılan yazım: "İstanbul (IST)"
    public string Etiket => $"{Sehir} ({Kod})";
}

// Haritada gösterilen havalimanları ve koordinatları
public static class Havalimanlari
{
    public static readonly IReadOnlyList<Havalimani> Liste = new List<Havalimani>
    {
        new("IST", "İstanbul", "İstanbul Havalimanı", 41.275, 28.752),
        new("SAW", "İstanbul", "Sabiha Gökçen", 40.898, 29.309),
        new("ESB", "Ankara", "Esenboğa", 40.128, 32.995),
        new("ADB", "İzmir", "Adnan Menderes", 38.292, 27.157),
        new("AYT", "Antalya", "Antalya Havalimanı", 36.899, 30.800),
        new("BJV", "Bodrum", "Milas-Bodrum", 37.250, 27.664),
        new("DLM", "Dalaman", "Dalaman Havalimanı", 36.713, 28.793),
        new("TZX", "Trabzon", "Trabzon Havalimanı", 40.995, 39.790),
        new("ADA", "Adana", "Adana Havalimanı", 36.982, 35.280),
        new("GZT", "Gaziantep", "Gaziantep Havalimanı", 36.947, 37.479),
        new("ASR", "Kayseri", "Erkilet", 38.770, 35.495),
        new("DIY", "Diyarbakır", "Diyarbakır Havalimanı", 37.894, 40.201),
        new("ERZ", "Erzurum", "Erzurum Havalimanı", 39.957, 41.170),
        new("VAN", "Van", "Ferit Melen", 38.468, 43.332),
        new("SZF", "Samsun", "Çarşamba", 41.255, 36.567),
        new("KYA", "Konya", "Konya Havalimanı", 37.979, 32.562),
        new("MLX", "Malatya", "Malatya Havalimanı", 38.435, 38.091),
        new("HTY", "Hatay", "Hatay Havalimanı", 36.363, 36.282),
        new("KSY", "Kars", "Harakani", 40.562, 43.115),
        new("GNY", "Şanlıurfa", "GAP Havalimanı", 37.445, 38.896),
        new("EZS", "Elazığ", "Elazığ Havalimanı", 38.607, 39.291),
        new("DNZ", "Denizli", "Çardak", 37.786, 29.701),
        new("CKZ", "Çanakkale", "Çanakkale Havalimanı", 40.138, 26.427),
        new("RZV", "Rize", "Rize-Artvin", 41.170, 40.850),

        new("LHR", "Londra", "Heathrow", 51.470, -0.454, true),
        new("CDG", "Paris", "Charles de Gaulle", 49.010, 2.550, true),
        new("FRA", "Frankfurt", "Frankfurt Havalimanı", 50.033, 8.571, true),
        new("AMS", "Amsterdam", "Schiphol", 52.310, 4.768, true),
        new("BER", "Berlin", "Brandenburg", 52.362, 13.500, true),
        new("SVO", "Moskova", "Şeremetyevo", 55.973, 37.415, true),
        new("DXB", "Dubai", "Dubai Havalimanı", 25.253, 55.365, true),
        new("GYD", "Bakü", "Haydar Aliyev", 40.467, 50.047, true),
        new("TBS", "Tiflis", "Tiflis Havalimanı", 41.669, 44.955, true),
    };

    // "İstanbul (IST)" -> IST havalimanı; kod yoksa şehir adına bakar
    public static Havalimani? Bul(string? metin)
    {
        if (string.IsNullOrWhiteSpace(metin))
            return null;

        int ac = metin.LastIndexOf('('), kapa = metin.LastIndexOf(')');
        if (ac >= 0 && kapa > ac)
        {
            var kod = metin[(ac + 1)..kapa].Trim().ToUpperInvariant();
            var bulunan = Liste.FirstOrDefault(h => h.Kod == kod);
            if (bulunan != null) return bulunan;
        }

        var sehir = SehirAdi(metin);
        return Liste.FirstOrDefault(h => string.Equals(h.Sehir, sehir, StringComparison.CurrentCultureIgnoreCase));
    }

    // "İstanbul (IST)" -> "İstanbul"
    public static string SehirAdi(string metin)
    {
        int ac = metin.IndexOf('(');
        return (ac > 0 ? metin[..ac] : metin).Trim();
    }
}
