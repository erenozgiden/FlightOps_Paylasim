namespace FlightOps.Models;

public enum BiletDurumu
{
    Aktif,
    Iptal
}

public class Bilet
{
    public int Id { get; set; }

    public int UcusId { get; set; }
    public Ucus? Ucus { get; set; }

    public int KullaniciId { get; set; }
    public Kullanici? Kullanici { get; set; }

    public int KoltukNo { get; set; }

    public decimal Fiyat { get; set; }

    public DateTime SatinAlmaTarihi { get; set; } = DateTime.Now;

    public BiletDurumu Durum { get; set; } = BiletDurumu.Aktif;

    // Ödeme bilgileri. Güvenlik için kart numarasının tamamı ve CVV saklanmaz,
    // yalnızca kart sahibi, kart tipi ve son 4 hane tutulur.
    public string? KartSahibi { get; set; }
    public string? KartTipi { get; set; }
    public string? KartSonDortHane { get; set; }

    public string OdemeBilgisi =>
        KartSonDortHane == null ? "-" : $"{KartTipi} •••• {KartSonDortHane}";
}
