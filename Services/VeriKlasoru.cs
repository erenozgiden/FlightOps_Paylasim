namespace FlightOps.Services;

// Veritabanı ve yüklenen fotoğraflar tek ve kalıcı bir klasörde tutulur:
//   %LOCALAPPDATA%\FlightOps   (örn. C:\Users\<ad>\AppData\Local\FlightOps)
// Böylece VS Code'da F5 ile de, masaüstü kısayolundan da açılsa aynı veriler kullanılır
// ve uygulama güncellendiğinde kayıtlar silinmez.
public class VeriKlasoru
{
    public string Yol { get; }
    public string Veritabani => Path.Combine(Yol, "flightops.db");
    public string Yuklemeler => Path.Combine(Yol, "yuklemeler");

    public VeriKlasoru()
    {
        Yol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlightOps");
        Directory.CreateDirectory(Yuklemeler);
    }

    // Eski sürümlerde veritabanı uygulamanın yanında duruyordu.
    // Yeni klasörde veritabanı yoksa en son kullanılan eski veritabanını (ve fotoğrafları) buraya kopyalar.
    public void EskiVerileriTasi(params string[] eskiKlasorler)
    {
        if (File.Exists(Veritabani))
            return;

        var eski = eskiKlasorler
            .Select(k => Path.Combine(k, "flightops.db"))
            .Where(File.Exists)
            .OrderByDescending(File.GetLastWriteTime)
            .FirstOrDefault();
        if (eski == null)
            return;

        // SQLite son değişiklikleri -wal dosyasında tutabilir; onları da kopyala
        File.Copy(eski, Veritabani);
        foreach (var ek in new[] { "-wal", "-shm" })
        {
            if (File.Exists(eski + ek))
                File.Copy(eski + ek, Veritabani + ek);
        }

        var eskiFotolar = Path.Combine(Path.GetDirectoryName(eski)!, "wwwroot", "yuklemeler");
        if (Directory.Exists(eskiFotolar))
        {
            foreach (var foto in Directory.GetFiles(eskiFotolar))
            {
                var hedef = Path.Combine(Yuklemeler, Path.GetFileName(foto));
                if (!File.Exists(hedef))
                    File.Copy(foto, hedef);
            }
        }

        Console.WriteLine($"Eski veriler taşındı: {eski} -> {Veritabani}");
    }
}
