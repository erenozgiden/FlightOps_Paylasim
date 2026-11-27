namespace FlightOps.Services;

// Kredi kartı doğrulama yardımcıları (okul projesi: gerçek ödeme alınmaz)
public static class KrediKarti
{
    // Boşluk ve tireleri atıp sadece rakamları bırakır
    public static string Temizle(string? numara) =>
        new string((numara ?? "").Where(char.IsDigit).ToArray());

    // Luhn algoritması: kart numarasının geçerli olup olmadığını kontrol eder
    public static bool LuhnGecerli(string rakamlar)
    {
        if (rakamlar.Length is < 13 or > 19)
            return false;

        int toplam = 0;
        bool ikiKat = false;
        for (int i = rakamlar.Length - 1; i >= 0; i--)
        {
            int r = rakamlar[i] - '0';
            if (ikiKat)
            {
                r *= 2;
                if (r > 9) r -= 9;
            }
            toplam += r;
            ikiKat = !ikiKat;
        }
        return toplam % 10 == 0;
    }

    // Numaranın başına bakarak kart tipini bulur
    public static string Tip(string rakamlar)
    {
        if (rakamlar.StartsWith("9792")) return "Troy";
        if (rakamlar.StartsWith("4")) return "Visa";
        if (rakamlar.StartsWith("34") || rakamlar.StartsWith("37")) return "Amex";
        if (rakamlar.Length >= 2 && int.TryParse(rakamlar[..2], out var iki) && iki is >= 51 and <= 55) return "Mastercard";
        if (rakamlar.Length >= 4 && int.TryParse(rakamlar[..4], out var dort) && dort is >= 2221 and <= 2720) return "Mastercard";
        return "Kart";
    }

    // Son kullanma tarihi geçmiş mi? (kart, belirtilen ayın sonuna kadar geçerlidir)
    public static bool SuresiGecmis(int ay, int yil) =>
        new DateTime(yil, ay, 1).AddMonths(1) <= DateTime.Today;
}
