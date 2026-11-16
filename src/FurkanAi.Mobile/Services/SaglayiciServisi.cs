using FurkanAi.Core.Saglayicilar;

namespace FurkanAi.Mobile.Services;

/// <summary>
/// Sağlayıcı yapılandırması. API anahtarları yalnızca SecureStorage'da (Android Keystore ile şifreli) tutulur;
/// koda, günlüğe, dışa aktarıma veya tercihlere asla yazılmaz.
/// </summary>
public sealed class SaglayiciServisi
{
    private readonly Ayarlar _ayar;
    private readonly Dictionary<SaglayiciTuru, bool> _anahtarVar = new();
    public event Action? Degisti;

    /// <summary>Akışlı yanıtlar uzun sürebilir; zaman aşımı istek başına iptal belirteciyle yönetilir.</summary>
    public static readonly HttpClient Http = new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5), ConnectTimeout = TimeSpan.FromSeconds(20) })
    { Timeout = TimeSpan.FromMinutes(6) };

    static SaglayiciServisi() => Http.DefaultRequestHeaders.UserAgent.ParseAdd("FurkanAI/1.0 (Android)");

    public SaglayiciServisi(Ayarlar ayar) => _ayar = ayar;

    private static string AnahtarAdi(SaglayiciTuru t) => "fa_anahtar_" + t;

    public async Task<bool> AnahtarVar(SaglayiciTuru t)
    {
        if (_anahtarVar.TryGetValue(t, out var v)) return v;
        v = !string.IsNullOrEmpty(await AnahtarOku(t));
        _anahtarVar[t] = v;
        return v;
    }

    private static async Task<string?> AnahtarOku(SaglayiciTuru t)
    {
        try { return await SecureStorage.Default.GetAsync(AnahtarAdi(t)); }
        catch (Exception e) { HataKaydi.Yaz("Güvenli depo okuma", e); return null; }
    }

    public async Task AnahtarKaydet(SaglayiciTuru t, string? anahtar)
    {
        if (string.IsNullOrWhiteSpace(anahtar)) SecureStorage.Default.Remove(AnahtarAdi(t));
        else await SecureStorage.Default.SetAsync(AnahtarAdi(t), anahtar.Trim());
        _anahtarVar[t] = !string.IsNullOrWhiteSpace(anahtar);
        Degisti?.Invoke();
    }

    /// <summary>Sağlayıcının kullanıma hazır olup olmadığı (anahtar/adres).</summary>
    public async Task<string?> HazirDegilNedeni(SaglayiciTuru t)
    {
        var tanim = ModelKaydi.Tanim(t);
        if (tanim.AnahtarGerekli && !await AnahtarVar(t)) return $"{tanim.Ad} API anahtarı girilmemiş.";
        if (t == SaglayiciTuru.OpenAIUyumlu && string.IsNullOrWhiteSpace(_ayar.TabanUrl(t))) return "OpenAI uyumlu sunucu adresi girilmemiş.";
        if (string.IsNullOrWhiteSpace(_ayar.Model(t))) return "Model seçilmemiş.";
        return null;
    }

    public async Task<SaglayiciIstemcisi> Istemci(SaglayiciTuru t) =>
        IstemciFabrikasi.Olustur(t, Http, await AnahtarOku(t), _ayar.TabanUrl(t));

    public static bool InternetVar => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    /// <summary>Yerel ağ sunucusu (Ollama vb.) için yalnızca yerel bağlantı yeterlidir.</summary>
    public bool BaglantiYeterli(SaglayiciTuru t) =>
        InternetVar || t == SaglayiciTuru.OpenAIUyumlu && Connectivity.Current.NetworkAccess is NetworkAccess.Local or NetworkAccess.ConstrainedInternet;
}
