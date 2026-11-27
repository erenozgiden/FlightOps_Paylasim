using System.ComponentModel.DataAnnotations;

namespace FlightOps.Models;

public enum UcusDurumu
{
    [Display(Name = "Planlandı")] Planlandi,
    [Display(Name = "Biniş")] Binis,
    [Display(Name = "Kalktı")] Kalkti,
    [Display(Name = "İndi")] Indi,
    [Display(Name = "Gecikmeli")] Gecikmeli,
    [Display(Name = "İptal")] Iptal
}

public class Ucus
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Uçuş no zorunludur."), StringLength(10)]
    [Display(Name = "Uçuş no")]
    public string UcusNo { get; set; } = "";

    [Required(ErrorMessage = "Kalkış zorunludur."), StringLength(40)]
    [Display(Name = "Kalkış")]
    public string Kalkis { get; set; } = "";

    [Required(ErrorMessage = "Varış zorunludur."), StringLength(40)]
    [Display(Name = "Varış")]
    public string Varis { get; set; } = "";

    [Display(Name = "Kalkış zamanı")]
    public DateTime KalkisZamani { get; set; } = DateTime.Today.AddDays(1).AddHours(9);

    [Display(Name = "Varış zamanı")]
    public DateTime VarisZamani { get; set; } = DateTime.Today.AddDays(1).AddHours(11);

    [StringLength(10)]
    [Display(Name = "Kapı")]
    public string? Kapi { get; set; }

    [Range(1, 600, ErrorMessage = "Kapasite 1 ile 600 arasında olmalı.")]
    [Display(Name = "Kapasite")]
    public int Kapasite { get; set; } = 180;

    [Range(0, 100000, ErrorMessage = "Fiyat 0 ile 100.000 arasında olmalı.")]
    [Display(Name = "Fiyat (₺)")]
    public decimal Fiyat { get; set; } = 1500;

    [Display(Name = "Durum")]
    public UcusDurumu Durum { get; set; } = UcusDurumu.Planlandi;

    public List<Bilet> Biletler { get; set; } = new();
}
