using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using FurkanAi.Core.Ortak;
using FurkanAi.Core.Saglayicilar;
using FurkanAi.Mobile.Platforms.Android;

namespace FurkanAi.Mobile.Services;

/// <summary>Dosya paylaşma/kaydetme.</summary>
public static class Disari
{
    public static async Task Paylas(byte[] veri, string dosyaAdi, string mime)
    {
        var klasor = Path.Combine(FileSystem.CacheDirectory, "paylas");
        Directory.CreateDirectory(klasor);
        foreach (var f in Directory.GetFiles(klasor).Where(f => File.GetCreationTimeUtc(f) < DateTime.UtcNow.AddHours(-6)))
            try { File.Delete(f); } catch { }
        var yol = Path.Combine(klasor, dosyaAdi);
        await File.WriteAllBytesAsync(yol, veri);
        await MainThread.InvokeOnMainThreadAsync(() => Share.Default.RequestAsync(new ShareFileRequest { Title = dosyaAdi, File = new ShareFile(yol, mime) }));
    }

    public static Task<bool> Kaydet(byte[] veri, string dosyaAdi, string mime) => AndroidDosya.Kaydet(dosyaAdi, mime, veri);

    public static Task MetinPaylas(string metin, string baslik = "FURKAN AI") =>
        Share.Default.RequestAsync(new ShareTextRequest { Text = metin, Title = baslik });
}

/// <summary>PIN (PBKDF2, güvenli depo) + isteğe bağlı biyometrik kilit.</summary>
public sealed class KilitServisi
{
    private const string Anahtar = "fa_pin";
    private readonly Ayarlar _ayar;
    private DateTime? _arka;
    private int _hatali;
    public bool Kilitli { get; private set; }
    public DateTime? BeklemeBitis { get; private set; }
    public event Action? Degisti;
    public KilitServisi(Ayarlar a) => _ayar = a;

    public int PinUzunlugu => Preferences.Default.Get("pinUzunluk", 4);
    public void Baslat() { if (_ayar.PinAktif) { Kilitli = true; Degisti?.Invoke(); } }
    public void ArkaPlana() => _arka = DateTime.UtcNow;
    public void OnPlana()
    {
        if (_ayar.PinAktif && _arka is DateTime z && DateTime.UtcNow - z >= TimeSpan.FromMinutes(Math.Max(0, _ayar.OtoKilitDakika))) { Kilitli = true; Degisti?.Invoke(); }
        _arka = null;
    }
    public void SimdiKilitle() { if (_ayar.PinAktif) { Kilitli = true; Degisti?.Invoke(); } }

    private static string Ozet(string pin, byte[] tuz) => Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(pin, tuz, 100_000, HashAlgorithmName.SHA256, 32));
    public static bool Gecerli(string pin) => pin.Length is >= 4 and <= 8 && pin.All(char.IsAsciiDigit) && pin.Distinct().Count() > 1;

    public async Task PinAyarla(string pin)
    {
        var tuz = RandomNumberGenerator.GetBytes(16);
        await SecureStorage.Default.SetAsync(Anahtar, Convert.ToBase64String(tuz) + ":" + Ozet(pin, tuz));
        Preferences.Default.Set("pinUzunluk", pin.Length);
        _ayar.PinAktif = true;
        _hatali = 0;
    }

    public void PinKaldir() { SecureStorage.Default.Remove(Anahtar); _ayar.PinAktif = false; _ayar.BiyometrikAktif = false; Kilitli = false; Degisti?.Invoke(); }

    /// <summary>5 hatalı denemeden sonra her denemede artan bekleme (30 sn, 60 sn …).</summary>
    public async Task<(bool Dogru, string? Mesaj)> Dogrula(string pin)
    {
        if (BeklemeBitis is DateTime b && b > DateTime.UtcNow) return (false, $"Çok fazla hatalı deneme. {Math.Ceiling((b - DateTime.UtcNow).TotalSeconds)} sn bekleyin.");
        var kayit = await SecureStorage.Default.GetAsync(Anahtar);
        if (kayit == null) { PinKaldir(); return (true, null); }
        var p = kayit.Split(':');
        var dogru = p.Length == 2 && CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(p[1]), Convert.FromBase64String(Ozet(pin, Convert.FromBase64String(p[0]))));
        if (dogru) { _hatali = 0; BeklemeBitis = null; Kilitli = false; Degisti?.Invoke(); return (true, null); }
        _hatali++;
        if (_hatali >= 5) BeklemeBitis = DateTime.UtcNow.AddSeconds(30 * (_hatali - 4));
        return (false, _hatali >= 5 ? "PIN hatalı. Bir süre beklemeniz gerekiyor." : $"PIN hatalı ({_hatali}/5).");
    }

    public bool BiyometrikKullanilabilir => _ayar.PinAktif && _ayar.BiyometrikAktif && Platforms.Android.AndroidBiyometri.Destekleniyor();

    /// <summary>Parmak izi/yüz ile kilidi açar. Başarısızsa PIN ekranı kalır.</summary>
    public async Task<bool> BiyometrikAc()
    {
        if (!BiyometrikKullanilabilir) return false;
        var ok = await Platforms.Android.AndroidBiyometri.Dogrula("FURKAN AI kilidini aç", "Parmak izi veya yüz tanıma");
        if (ok) { _hatali = 0; BeklemeBitis = null; Kilitli = false; Degisti?.Invoke(); }
        return ok;
    }
}

/// <summary>Uygulama açılışı ve ön/arka plan geçişleri.</summary>
public sealed class UygulamaYasamDongusu
{
    private readonly IServiceProvider _sp;
    private bool _basladi;
    public bool ArkaPlanda { get; private set; }
    public UygulamaYasamDongusu(IServiceProvider sp) => _sp = sp;
    private T S<T>() where T : notnull => _sp.GetRequiredService<T>();

    public async Task Baslat()
    {
        if (_basladi) return;
        _basladi = true;
        S<KilitServisi>().Baslat();
        try { await S<Veritabani>().Baglanti(); }
        catch (Exception e) { HataKaydi.Yaz("Veritabanı", e); }
    }

    public void ArkaPlana() { ArkaPlanda = true; S<KilitServisi>().ArkaPlana(); S<SesServisi>().Sustur(); }
    public void OnPlana() { ArkaPlanda = false; S<KilitServisi>().OnPlana(); }
}
