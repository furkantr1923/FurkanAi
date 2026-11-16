using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FurkanAi.Core.Saglayicilar;

public sealed record UretilenGorsel(byte[] Veri, string Mime, string? Aciklama);

/// <summary>Tüm sağlayıcı istemcilerinin ortak tabanı: HTTP gönderimi, hata çevirisi, SSE akışı.</summary>
public abstract class SaglayiciIstemcisi
{
    protected readonly HttpClient Http;
    protected readonly string? Anahtar;
    protected readonly string TabanUrl;
    public SaglayiciTuru Tur { get; }
    public string Ad => ModelKaydi.Tanim(Tur).Ad;

    protected SaglayiciIstemcisi(SaglayiciTuru tur, HttpClient http, string? anahtar, string? tabanUrl)
    {
        Tur = tur;
        Http = http;
        Anahtar = string.IsNullOrWhiteSpace(anahtar) ? null : anahtar.Trim();
        TabanUrl = (string.IsNullOrWhiteSpace(tabanUrl) ? ModelKaydi.Tanim(Tur).VarsayilanTabanUrl : tabanUrl.Trim()).TrimEnd('/');
        if (ModelKaydi.Tanim(Tur).AnahtarGerekli && Anahtar == null)
            throw new SaglayiciHatasi($"{Ad} için API anahtarı girilmemiş. Ayarlar › Sağlayıcılar ekranından ekleyin.");
    }

    /// <summary>Yanıtı parça parça (token akışı) döndürür.</summary>
    public abstract IAsyncEnumerable<string> Akis(IReadOnlyList<SohbetMesaji> mesajlar, IstekSecenekleri s, CancellationToken ct);

    public abstract Task<List<string>> ModelleriGetir(CancellationToken ct);

    public virtual Task<UretilenGorsel> GorselUret(string istem, string model, string boyut, CancellationToken ct) =>
        throw new SaglayiciHatasi($"{Ad} ile görsel üretimi bu uygulamada desteklenmiyor. OpenAI veya Gemini seçin.");

    /// <summary>Bağlantı testi: model listesini çeker (ücretsiz uç nokta).</summary>
    public async Task<string> BaglantiTesti(CancellationToken ct)
    {
        var l = await ModelleriGetir(ct);
        return $"Bağlantı başarılı ✅ {l.Count} model erişilebilir.";
    }

    protected abstract void Kimlik(HttpRequestMessage istek);

    protected HttpRequestMessage Istek(HttpMethod yontem, string yol, JsonNode? govde = null)
    {
        var i = new HttpRequestMessage(yontem, yol.StartsWith("http", StringComparison.Ordinal) ? yol : TabanUrl + yol);
        Kimlik(i);
        if (govde != null) i.Content = new StringContent(govde.ToJsonString(), Encoding.UTF8, "application/json");
        return i;
    }

    protected async Task<HttpResponseMessage> Gonder(HttpRequestMessage istek, CancellationToken ct, bool akis = false)
    {
        HttpResponseMessage y;
        try { y = await Http.SendAsync(istek, akis ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (TaskCanceledException e) { throw new SaglayiciHatasi($"{Ad} yanıt vermedi (zaman aşımı).", 408, true, e); }
        catch (HttpRequestException e) { throw new SaglayiciHatasi($"{Ad} sunucusuna ulaşılamadı. İnternet bağlantınızı{(Tur == SaglayiciTuru.OpenAIUyumlu ? " ve sunucu adresini" : "")} kontrol edin.", null, true, e); }
        if (y.IsSuccessStatusCode) return y;
        string? govde = null;
        try { govde = await y.Content.ReadAsStringAsync(ct); } catch { }
        var kod = (int)y.StatusCode;
        y.Dispose();
        throw SaglayiciHatasi.Olustur(Ad, kod, HataMetni(govde));
    }

    /// <summary>Sağlayıcıların hata JSON'larından ({"error":{"message":..}} vb.) mesajı çıkarır.</summary>
    internal static string? HataMetni(string? govde)
    {
        if (string.IsNullOrWhiteSpace(govde)) return null;
        try
        {
            var n = JsonNode.Parse(govde);
            if (n is JsonArray a && a.Count > 0) n = a[0];
            var e = n?["error"];
            return e switch
            {
                JsonObject o => o["message"]?.GetValue<string>() ?? o.ToJsonString(),
                JsonValue v => v.ToString(),
                _ => n?["message"]?.GetValue<string>() ?? Ortak.S.Kisalt(govde, 200)
            };
        }
        catch (JsonException) { return Ortak.S.Kisalt(govde, 200); }
    }

    protected async IAsyncEnumerable<SseOlayi> SseAkisi(HttpRequestMessage istek, [EnumeratorCancellation] CancellationToken ct)
    {
        istek.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        using var y = await Gonder(istek, ct, akis: true);
        await using var s = await y.Content.ReadAsStreamAsync(ct);
        await foreach (var o in SseOkuyucu.Oku(s, ct)) yield return o;
    }

    protected static string DataUrl(EkIcerik e) => $"data:{e.Mime};base64,{Convert.ToBase64String(e.Veri)}";

    /// <summary>Metin tabanlı eki (TXT/DOCX/çıkarılmış PDF) modele okunabilir blok olarak verir.</summary>
    internal static string MetinEki(EkIcerik e) =>
        $"📎 Ekli dosya: {e.Ad}\n```\n{Dosyalar.DosyaOkuyucu.BaglamIcinKirp(e.Metin ?? "")}\n```";
}

public static class IstemciFabrikasi
{
    public static SaglayiciIstemcisi Olustur(SaglayiciTuru t, HttpClient http, string? anahtar, string? tabanUrl) => t switch
    {
        SaglayiciTuru.OpenAI => new OpenAiIstemcisi(http, anahtar, tabanUrl, uyumlu: false),
        SaglayiciTuru.OpenAIUyumlu => new OpenAiIstemcisi(http, anahtar, tabanUrl, uyumlu: true),
        SaglayiciTuru.Gemini => new GeminiIstemcisi(http, anahtar, tabanUrl),
        SaglayiciTuru.Claude => new ClaudeIstemcisi(http, anahtar, tabanUrl),
        _ => throw new ArgumentOutOfRangeException(nameof(t))
    };
}
