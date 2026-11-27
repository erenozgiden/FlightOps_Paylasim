using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FlightOps.ViewModels;

// Kullanıcı adında Türkçe harfler de kullanılabilir (ç, ğ, ı, ö, ş, ü)
public static class KullaniciAdiKurali
{
    public const string Desen = @"^[a-zA-Z0-9._çğıöşüÇĞİÖŞÜ]+$";
    public const string Mesaj = "Sadece harf, rakam, nokta ve alt çizgi kullanın (boşluk olmadan).";
}

public class SifremiUnuttumModel
{
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [Display(Name = "Kullanıcı adı")]
    public string KullaniciAdi { get; set; } = "";

    [Required(ErrorMessage = "Kayıtlı e-posta adresiniz zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [Display(Name = "Kayıtlı e-posta")]
    public string Eposta { get; set; } = "";

    [Required(ErrorMessage = "Yeni şifre zorunludur."), StringLength(64, MinimumLength = 4, ErrorMessage = "Şifre en az 4 karakter olmalı.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni şifre")]
    public string YeniSifre { get; set; } = "";

    [Compare(nameof(YeniSifre), ErrorMessage = "Şifreler aynı değil.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni şifre (tekrar)")]
    public string YeniSifreTekrar { get; set; } = "";
}

public class GirisModel
{
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [Display(Name = "Kullanıcı adı")]
    public string KullaniciAdi { get; set; } = "";

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Sifre { get; set; } = "";

    public string? DonusAdresi { get; set; }
}

public class KayitModel
{
    [Required(ErrorMessage = "Ad soyad zorunludur."), StringLength(80)]
    [Display(Name = "Ad soyad")]
    public string AdSoyad { get; set; } = "";

    [Required(ErrorMessage = "Kullanıcı adı zorunludur."), StringLength(40, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-40 karakter olmalı.")]
    [RegularExpression(KullaniciAdiKurali.Desen, ErrorMessage = KullaniciAdiKurali.Mesaj)]
    [Display(Name = "Kullanıcı adı")]
    public string KullaniciAdi { get; set; } = "";

    // Şifremi unuttum işleminde kimlik doğrulamak için kullanılır
    [Required(ErrorMessage = "E-posta zorunludur (şifrenizi unutursanız gerekir).")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [Display(Name = "E-posta")]
    public string? Eposta { get; set; }

    [Required(ErrorMessage = "Şifre zorunludur."), StringLength(64, MinimumLength = 4, ErrorMessage = "Şifre en az 4 karakter olmalı.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Sifre { get; set; } = "";

    [Compare(nameof(Sifre), ErrorMessage = "Şifreler aynı değil.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre (tekrar)")]
    public string SifreTekrar { get; set; } = "";

    [Required(ErrorMessage = "Ev adresi zorunludur."), StringLength(300, MinimumLength = 10, ErrorMessage = "Adres en az 10 karakter olmalı.")]
    [Display(Name = "Ev adresi")]
    public string Adres { get; set; } = "";

    [Required(ErrorMessage = "Profil fotoğrafı zorunludur.")]
    [Display(Name = "Profil fotoğrafı")]
    public IFormFile? Foto { get; set; }
}

public class ProfilModel
{
    [Display(Name = "Kullanıcı adı")]
    public string KullaniciAdi { get; set; } = "";

    [Required(ErrorMessage = "Ad soyad zorunludur."), StringLength(80)]
    [Display(Name = "Ad soyad")]
    public string AdSoyad { get; set; } = "";

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin."), StringLength(100)]
    [Display(Name = "E-posta")]
    public string? Eposta { get; set; }

    [Phone(ErrorMessage = "Geçerli bir telefon girin."), StringLength(20)]
    [Display(Name = "Telefon")]
    public string? Telefon { get; set; }

    [Required(ErrorMessage = "Ev adresi zorunludur."), StringLength(300, MinimumLength = 10, ErrorMessage = "Adres en az 10 karakter olmalı.")]
    [Display(Name = "Ev adresi")]
    public string? Adres { get; set; }

    // Fotoğraf yoksa yüklemek zorunlu; varsa değiştirmek isteğe bağlı
    [Display(Name = "Profil fotoğrafı")]
    public IFormFile? Foto { get; set; }

    public string? MevcutFotoYolu { get; set; }
}

public class SifreDegistirModel
{
    [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mevcut şifre")]
    public string MevcutSifre { get; set; } = "";

    [Required(ErrorMessage = "Yeni şifre zorunludur."), StringLength(64, MinimumLength = 4, ErrorMessage = "Şifre en az 4 karakter olmalı.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni şifre")]
    public string YeniSifre { get; set; } = "";

    [Compare(nameof(YeniSifre), ErrorMessage = "Şifreler aynı değil.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni şifre (tekrar)")]
    public string YeniSifreTekrar { get; set; } = "";
}
