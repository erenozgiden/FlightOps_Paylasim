using System.ComponentModel.DataAnnotations;

namespace FlightOps.Models;

public enum Rol
{
    Admin,
    Personel,
    Kullanici
}

// Sisteme giriş yapabilen herkes: admin, personel ve normal kullanıcılar
public class Kullanici
{
    public int Id { get; set; }

    [Required, StringLength(40)]
    [Display(Name = "Kullanıcı adı")]
    public string KullaniciAdi { get; set; } = "";

    [Required]
    public string SifreHash { get; set; } = "";

    [Required(ErrorMessage = "Ad soyad zorunludur."), StringLength(80)]
    [Display(Name = "Ad soyad")]
    public string AdSoyad { get; set; } = "";

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin."), StringLength(100)]
    [Display(Name = "E-posta")]
    public string? Eposta { get; set; }

    [Phone(ErrorMessage = "Geçerli bir telefon girin."), StringLength(20)]
    [Display(Name = "Telefon")]
    public string? Telefon { get; set; }

    public Rol Rol { get; set; } = Rol.Kullanici;

    // Personel için görev (Pilot, Kabin Memuru, Yer Hizmetleri ...)
    [StringLength(50)]
    [Display(Name = "Görev")]
    public string? Gorev { get; set; }

    // Profil fotoğrafının adresi (örn. /yuklemeler/abc.jpg)
    public string? FotoYolu { get; set; }

    // Kullanıcılar için zorunlu ev adresi
    [StringLength(300)]
    [Display(Name = "Ev adresi")]
    public string? Adres { get; set; }

    // Kullanıcı rolündekilerin profil fotoğrafı ve ev adresi zorunludur
    public bool ProfilEksik => Rol == Rol.Kullanici
        && (string.IsNullOrWhiteSpace(FotoYolu) || string.IsNullOrWhiteSpace(Adres));

    public DateTime KayitTarihi { get; set; } = DateTime.Now;

    public List<Bilet> Biletler { get; set; } = new();
}
