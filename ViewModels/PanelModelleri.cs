using System.ComponentModel.DataAnnotations;
using FlightOps.Models;
using Microsoft.AspNetCore.Http;

namespace FlightOps.ViewModels;

// Admin panelinde personel ekleme / düzenleme formu
public class PersonelFormModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ad soyad zorunludur."), StringLength(80)]
    [Display(Name = "Ad soyad")]
    public string AdSoyad { get; set; } = "";

    [Required(ErrorMessage = "Kullanıcı adı zorunludur."), StringLength(40, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-40 karakter olmalı.")]
    [RegularExpression(KullaniciAdiKurali.Desen, ErrorMessage = KullaniciAdiKurali.Mesaj)]
    [Display(Name = "Kullanıcı adı")]
    public string KullaniciAdi { get; set; } = "";

    [StringLength(50)]
    [Display(Name = "Görev")]
    public string? Gorev { get; set; }

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [Display(Name = "E-posta")]
    public string? Eposta { get; set; }

    [Phone(ErrorMessage = "Geçerli bir telefon girin.")]
    [Display(Name = "Telefon")]
    public string? Telefon { get; set; }

    // Eklerken zorunlu, düzenlerken boş bırakılırsa değişmez
    [StringLength(64, MinimumLength = 4, ErrorMessage = "Şifre en az 4 karakter olmalı.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string? Sifre { get; set; }

    [Display(Name = "Fotoğraf")]
    public IFormFile? Foto { get; set; }

    public string? MevcutFotoYolu { get; set; }

    [Display(Name = "Fotoğrafı kaldır")]
    public bool FotoyuKaldir { get; set; }
}

// Admin panelinde kullanıcı listesi satırı
public class KullaniciSatiri
{
    public Kullanici Kullanici { get; set; } = null!;
    public int AktifBilet { get; set; }
}

// Kullanıcı panelinde uçuş filtreleme
public class UcusFiltreModel
{
    [Display(Name = "Kalkış")]
    public string? Kalkis { get; set; }

    [Display(Name = "Varış")]
    public string? Varis { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Tarih")]
    public DateTime? Tarih { get; set; }

    [Display(Name = "Durum")]
    public UcusDurumu? Durum { get; set; }

    public List<UcusSatiri> Ucuslar { get; set; } = new();

    // Nereden / Nereye seçim listesi (uçuşu olan şehirler)
    public List<string> Sehirler { get; set; } = new();

    public HaritaModel Harita { get; set; } = new();
}

// Türkiye haritasına gönderilen veriler (JSON olarak sayfaya gömülür)
public class HaritaModel
{
    public List<HaritaHavalimani> Havalimanlari { get; set; } = new();
    public List<HaritaUcusu> Ucuslar { get; set; } = new();
    public string? SeciliKalkis { get; set; }
    public string? SeciliVaris { get; set; }
}

public record HaritaHavalimani(string Kod, string Sehir, string Ad, double Enlem, double Boylam, bool YurtDisi);

public record HaritaUcusu(
    int Id, string UcusNo, string? KalkisKod, string? VarisKod,
    string KalkisSehir, string VarisSehir, string Zaman,
    string Durum, string DurumSinifi, string Fiyat, bool Eslesti);

public class UcusSatiri
{
    public Ucus Ucus { get; set; } = null!;
    public int SatilanKoltuk { get; set; }
    public int BosKoltuk => Math.Max(0, Ucus.Kapasite - SatilanKoltuk);
    public bool BiletAlinabilir { get; set; }
    public bool KullanicininBileti { get; set; }
}

// Kullanıcı panelinde bilet alırken ödeme formu
public class OdemeModel
{
    public int UcusId { get; set; }

    [Required(ErrorMessage = "Kart üzerindeki isim zorunludur."), StringLength(60)]
    [Display(Name = "Kart üzerindeki isim")]
    public string KartSahibi { get; set; } = "";

    [Required(ErrorMessage = "Kart numarası zorunludur.")]
    [Display(Name = "Kart numarası")]
    public string KartNumarasi { get; set; } = "";

    [Range(1, 12, ErrorMessage = "Ay seçin.")]
    [Display(Name = "Ay")]
    public int SonKullanmaAy { get; set; }

    [Range(2000, 2100, ErrorMessage = "Yıl seçin.")]
    [Display(Name = "Yıl")]
    public int SonKullanmaYil { get; set; }

    [Required(ErrorMessage = "CVV zorunludur.")]
    [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV 3 veya 4 rakam olmalı.")]
    [Display(Name = "CVV")]
    public string Cvv { get; set; } = "";

    // Sayfada gösterilen uçuş özeti
    public Ucus? Ucus { get; set; }
    public int BosKoltuk { get; set; }
}

// Admin paneli raporları
public class RaporModel
{
    public int ToplamUcus { get; set; }
    public int ToplamPersonel { get; set; }
    public int ToplamKullanici { get; set; }
    public int AktifBilet { get; set; }
    public int IptalBilet { get; set; }
    public decimal ToplamGelir { get; set; }
    public double OrtalamaDoluluk { get; set; }

    public List<(UcusDurumu Durum, int Sayi)> DurumDagilimi { get; set; } = new();
    public List<UcusRaporSatiri> UcusBazinda { get; set; } = new();
    public List<(string AdSoyad, int BiletSayisi, decimal Harcama)> EnCokBiletAlanlar { get; set; } = new();
    public List<(DateTime Gun, int Adet, decimal Tutar)> SonYediGun { get; set; } = new();
}

public class UcusRaporSatiri
{
    public string UcusNo { get; set; } = "";
    public string Guzergah { get; set; } = "";
    public DateTime KalkisZamani { get; set; }
    public UcusDurumu Durum { get; set; }
    public int Kapasite { get; set; }
    public int SatilanBilet { get; set; }
    public decimal Gelir { get; set; }
    public double Doluluk => Kapasite == 0 ? 0 : (double)SatilanBilet / Kapasite;
}
