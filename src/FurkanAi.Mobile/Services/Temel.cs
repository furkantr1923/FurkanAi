using System.Text.Json;
using System.Text.Json.Serialization;
using FurkanAi.Core.Saglayicilar;

namespace FurkanAi.Mobile.Services;

public static class Json
{
    public static readonly JsonSerializerOptions Secenek = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    public static string Yaz<T>(T v) => JsonSerializer.Serialize(v, Secenek);
    public static T? Oku<T>(string? s) { if (string.IsNullOrEmpty(s)) return default; try { return JsonSerializer.Deserialize<T>(s, Secenek); } catch (JsonException) { return default; } }
}

public static class UygulamaBilgisi
{
    /// <summary>Güncelleme denetimi için GitHub deposu (sahip/ad). Kendi deponuzu yazın.</summary>
    public const string GithubDeposu = "furkantr1923/FURKAN-AI";
    public static string Surum => AppInfo.Current.VersionString;
    public static string Derleme => AppInfo.Current.BuildString;
    public static string Cihaz => $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model} · Android {DeviceInfo.Current.VersionString}";
}

/// <summary>Kullanıcı tercihleri (Preferences). API anahtarları BURADA DEĞİL, SecureStorage'da tutulur.</summary>
public sealed class Ayarlar
{
    private static T Al<T>(string k, T v) => Preferences.Default.Get(k, v);
    private void Koy<T>(string k, T v) { Preferences.Default.Set(k, v); Degisti?.Invoke(); }
    public event Action? Degisti;

    // Görünüm
    public string Tema { get => Al("tema", "koyu"); set => Koy("tema", value); }            // koyu | acik | sistem
    public string Vurgu { get => Al("vurgu", "mor"); set => Koy("vurgu", value); }          // siyan | mor | pembe | yesil
    public bool AzAnimasyon { get => Al("azAnimasyon", false); set => Koy("azAnimasyon", value); }
    public string YaziBoyutu { get => Al("yaziBoyutu", "normal"); set => Koy("yaziBoyutu", value); } // kucuk | normal | buyuk

    // Sohbet
    public SaglayiciTuru AktifSaglayici { get => Enum.TryParse<SaglayiciTuru>(Al("saglayici", ""), out var t) ? t : SaglayiciTuru.Gemini; set => Koy("saglayici", value.ToString()); }
    public string Model(SaglayiciTuru t) => Al("model_" + t, ModelKaydi.VarsayilanModel(t));
    public void ModelAyarla(SaglayiciTuru t, string m) => Koy("model_" + t, m.Trim());
    public string? TabanUrl(SaglayiciTuru t) => Al<string?>("taban_" + t, null);
    public void TabanUrlAyarla(SaglayiciTuru t, string? u) { if (string.IsNullOrWhiteSpace(u)) Preferences.Default.Remove("taban_" + t); else Preferences.Default.Set("taban_" + t, u.Trim()); Degisti?.Invoke(); }
    /// <summary>Elle eklenen / "Modelleri getir" ile alınan model listesi.</summary>
    public List<string> ModelListesi(SaglayiciTuru t) => Al("modeller_" + t, "").Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();
    public void ModelListesiAyarla(SaglayiciTuru t, IEnumerable<string> l) => Koy("modeller_" + t, string.Join('|', l.Take(300)));
    public double Sicaklik { get => Al("sicaklik", -1.0); set => Koy("sicaklik", value); }    // -1 = sağlayıcı varsayılanı
    public int AzamiToken { get => Al("azamiToken", 4096); set => Koy("azamiToken", value); }
    public int BaglamMesajSayisi { get => Al("baglam", 20); set => Koy("baglam", value); }
    public string KisiselIstem { get => Al("kisiselIstem", ""); set => Koy("kisiselIstem", value); }
    public string YanitDili { get => Al("yanitDili", "otomatik"); set => Koy("yanitDili", value); }
    public bool GecmisiKaydet { get => Al("gecmisiKaydet", true); set => Koy("gecmisiKaydet", value); }
    public bool HafizaAktif { get => Al("hafiza", true); set => Koy("hafiza", value); }
    public bool DosyaOnayiSor { get => Al("dosyaOnay", true); set => Koy("dosyaOnay", value); }

    // Görsel
    public SaglayiciTuru GorselSaglayici { get => Enum.TryParse<SaglayiciTuru>(Al("gorselSaglayici", ""), out var t) ? t : SaglayiciTuru.OpenAI; set => Koy("gorselSaglayici", value.ToString()); }
    public string GorselModel(SaglayiciTuru t) => Al("gorselModel_" + t, ModelKaydi.GorselModelleri(t).FirstOrDefault()?.Id ?? "");
    public void GorselModelAyarla(SaglayiciTuru t, string m) => Koy("gorselModel_" + t, m.Trim());
    public string GorselBoyut { get => Al("gorselBoyut", "1024x1024"); set => Koy("gorselBoyut", value); }

    // Ses
    public string SttMotoru { get => Al("stt", "cihaz"); set => Koy("stt", value); }         // cihaz | openai
    public string TtsMotoru { get => Al("tts", "cihaz"); set => Koy("tts", value); }         // cihaz | openai
    public bool OtomatikSesliOku { get => Al("otoSesli", false); set => Koy("otoSesli", value); }
    public string SesDili { get => Al("sesDili", "tr-TR"); set => Koy("sesDili", value); }
    public string OpenAiSes { get => Al("openaiSes", "alloy"); set => Koy("openaiSes", value); }
    public double KonusmaHizi { get => Al("konusmaHizi", 1.0); set => Koy("konusmaHizi", value); }

    // Bildirim ve güvenlik
    public bool YanitBildirimi { get => Al("yanitBildirim", true); set => Koy("yanitBildirim", value); }
    public bool PinAktif { get => Al("pinAktif", false); set => Koy("pinAktif", value); }
    public bool BiyometrikAktif { get => Al("biyometrik", false); set => Koy("biyometrik", value); }
    public int OtoKilitDakika { get => Al("otoKilit", 1); set => Koy("otoKilit", value); }

    public bool TanitimGoruldu { get => Al("tanitim", false); set => Koy("tanitim", value); }

    public void Sifirla()
    {
        // PIN/biyometri durumu, tanıtım ve sağlayıcı bağlantı ayarları korunur
        var koru = new[] { "saglayici" }
            .Concat(Enum.GetNames<SaglayiciTuru>().SelectMany(t => new[] { "model_" + t, "taban_" + t, "modeller_" + t }))
            .Where(k => Preferences.Default.ContainsKey(k)).ToDictionary(k => k, k => Preferences.Default.Get(k, ""));
        var pin = (PinAktif, Biyo: BiyometrikAktif, Uz: Preferences.Default.Get("pinUzunluk", 4), Tanitim: TanitimGoruldu);
        Preferences.Default.Clear();
        foreach (var (k, v) in koru) Preferences.Default.Set(k, v);
        Preferences.Default.Set("pinAktif", pin.PinAktif); Preferences.Default.Set("biyometrik", pin.Biyo);
        Preferences.Default.Set("pinUzunluk", pin.Uz); Preferences.Default.Set("tanitim", pin.Tanitim);
        Degisti?.Invoke();
    }
}

/// <summary>Uygulama içi bildirim (toast), onay diyaloğu ve genel olaylar.</summary>
public sealed class Arayuz
{
    public sealed record Toast(Guid Id, string Mesaj, string Tur);
    public sealed record Onay(string Baslik, string Mesaj, string Evet, string Hayir, bool Tehlikeli, TaskCompletionSource<bool> Sonuc);

    public event Action<Toast>? ToastGeldi;
    public event Action<Onay>? OnayIstendi;
    public event Action? VeriDegisti;

    public void Goster(string mesaj, string tur = "bilgi") => ToastGeldi?.Invoke(new Toast(Guid.NewGuid(), mesaj, tur));

    /// <summary>Uygulama tasarımında onay diyaloğu gösterir.</summary>
    public Task<bool> Onayla(string baslik, string mesaj, string evet = "Evet", string hayir = "Vazgeç", bool tehlikeli = false)
    {
        var tcs = new TaskCompletionSource<bool>();
        if (OnayIstendi == null) tcs.SetResult(false);
        else OnayIstendi.Invoke(new Onay(baslik, mesaj, evet, hayir, tehlikeli, tcs));
        return tcs.Task;
    }

    public void VeriDegistiBildir() => VeriDegisti?.Invoke();
}

/// <summary>Bildirim ve kısayollardan gelen sayfa yönlendirmeleri.</summary>
public static class Yonlendirme
{
    private static string? _bekleyen;
    public static event Action<string>? Istendi;
    public static void Iste(string yol) { if (Istendi == null) _bekleyen = yol; else Istendi(yol); }
    public static string? BekleyeniAl() { var b = _bekleyen; _bekleyen = null; return b; }
}

/// <summary>Android geri tuşu: önce açık katmanlar (alt sayfa/diyalog), sonra sayfa geçmişi.</summary>
public static class GeriTusu
{
    private static readonly List<Func<bool>> _yigin = new();
    public static Func<bool>? SayfaGeri { get; set; }
    public static void Ekle(Func<bool> f) { lock (_yigin) _yigin.Add(f); }
    public static void Cikar(Func<bool> f) { lock (_yigin) _yigin.Remove(f); }
    public static bool Isle()
    {
        Func<bool>? ust;
        lock (_yigin) ust = _yigin.Count > 0 ? _yigin[^1] : null;
        return ust != null ? ust() : SayfaGeri?.Invoke() ?? false;
    }
}

/// <summary>Beklenmeyen hataları cihazdaki günlük dosyasına yazar (çökme yerine kontrollü kayıt). Veri dışarı gönderilmez.</summary>
public static class HataKaydi
{
    private static readonly object _kilit = new();
    public static string Dosya => Path.Combine(FileSystem.AppDataDirectory, "hatalar.log");

    public static void Kur()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Yaz("İşlenmeyen", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) => { Yaz("Gözlemlenmeyen görev", e.Exception); e.SetObserved(); };
    }

    public static void Yaz(string kaynak, Exception? e)
    {
        try
        {
            lock (_kilit)
            {
                var f = new FileInfo(Dosya);
                if (f.Exists && f.Length > 256 * 1024) File.Move(Dosya, Dosya + ".1", true);
                File.AppendAllText(Dosya, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {kaynak}: {e?.GetType().Name}: {e?.Message}\n{e?.StackTrace}\n\n");
            }
        }
        catch { /* günlük yazılamazsa sessizce geç */ }
    }

    public static string Oku() { try { return File.Exists(Dosya) ? File.ReadAllText(Dosya) : ""; } catch { return ""; } }
    public static void Temizle() { try { File.Delete(Dosya); File.Delete(Dosya + ".1"); } catch { } }
}

/// <summary>Kullanıcı dostu hata mesajı üretimi (neden + çözüm önerisi).</summary>
public static class HataMesaji
{
    public static string Olustur(Exception e) => e switch
    {
        SaglayiciHatasi => e.Message,
        InvalidDataException => e.Message,
        PermissionException => "Gerekli izin verilmedi. Ayarlar › İzinler bölümünden izin verebilirsiniz.",
        FeatureNotSupportedException => "Bu özellik cihazınızda desteklenmiyor.",
        HttpRequestException or TaskCanceledException when Connectivity.Current.NetworkAccess != NetworkAccess.Internet => "İnternet bağlantısı yok. Bağlantınızı kontrol edip tekrar deneyin.",
        HttpRequestException => "Sunucuya ulaşılamadı. Biraz sonra tekrar deneyin.",
        TaskCanceledException => "İşlem zaman aşımına uğradı. Tekrar deneyin.",
        UnauthorizedAccessException or IOException => "Dosya okunamadı veya yazılamadı. Farklı bir konum seçip tekrar deneyin.",
        _ => e.Message
    };
}
