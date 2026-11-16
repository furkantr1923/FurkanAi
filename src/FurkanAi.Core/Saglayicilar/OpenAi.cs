using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace FurkanAi.Core.Saglayicilar;

/// <summary>OpenAI Chat Completions (ve OpenAI uyumlu sunucular: Ollama, LM Studio, OpenRouter, Groq…).</summary>
public sealed class OpenAiIstemcisi : SaglayiciIstemcisi
{
    private readonly bool _uyumlu;

    public OpenAiIstemcisi(HttpClient http, string? anahtar, string? tabanUrl, bool uyumlu)
        : base(uyumlu ? SaglayiciTuru.OpenAIUyumlu : SaglayiciTuru.OpenAI, http, anahtar, tabanUrl) => _uyumlu = uyumlu;

    protected override void Kimlik(HttpRequestMessage i)
    {
        if (Anahtar != null) i.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Anahtar);
    }

    internal static JsonObject GovdeOlustur(IReadOnlyList<SohbetMesaji> mesajlar, IstekSecenekleri s, bool uyumlu)
    {
        var dizi = new JsonArray();
        if (!string.IsNullOrWhiteSpace(s.SistemIstemi)) dizi.Add(new JsonObject { ["role"] = "system", ["content"] = s.SistemIstemi });
        foreach (var m in mesajlar)
        {
            if (m.Rol == Rol.Sistem) { dizi.Add(new JsonObject { ["role"] = "system", ["content"] = m.Metin }); continue; }
            var rol = m.Rol == Rol.Asistan ? "assistant" : "user";
            if (m.EkListesi.Count == 0 || m.Rol == Rol.Asistan) { dizi.Add(new JsonObject { ["role"] = rol, ["content"] = m.Metin }); continue; }
            var parcalar = new JsonArray();
            foreach (var e in m.EkListesi)
            {
                switch (e.Tur)
                {
                    case EkTuru.Gorsel:
                        parcalar.Add(new JsonObject { ["type"] = "image_url", ["image_url"] = new JsonObject { ["url"] = DataUrl(e) } });
                        break;
                    case EkTuru.Pdf when !uyumlu:
                        parcalar.Add(new JsonObject { ["type"] = "file", ["file"] = new JsonObject { ["filename"] = e.Ad, ["file_data"] = DataUrl(e) } });
                        break;
                    default:
                        if (!string.IsNullOrEmpty(e.Metin)) parcalar.Add(new JsonObject { ["type"] = "text", ["text"] = MetinEki(e) });
                        break;
                }
            }
            if (!string.IsNullOrWhiteSpace(m.Metin)) parcalar.Add(new JsonObject { ["type"] = "text", ["text"] = m.Metin });
            dizi.Add(new JsonObject { ["role"] = rol, ["content"] = parcalar });
        }
        var g = new JsonObject { ["model"] = s.Model, ["messages"] = dizi, ["stream"] = true };
        if (!uyumlu) g["stream_options"] = new JsonObject { ["include_usage"] = true };
        if (s.Sicaklik is double t) g["temperature"] = t;
        return g;
    }

    /// <summary>Bir SSE "data" satırını çözer: metin parçası, hata veya bitiş.</summary>
    internal static (string? Metin, string? Hata, bool Bitti) DeltaCoz(string veri)
    {
        if (veri.Trim() == "[DONE]") return (null, null, true);
        var n = JsonNode.Parse(veri);
        if (n?["error"] is JsonNode h) return (null, h["message"]?.GetValue<string>() ?? h.ToJsonString(), true);
        var secim = n?["choices"]?.AsArray().FirstOrDefault();
        var metin = secim?["delta"]?["content"]?.GetValue<string>();
        return (metin, null, false);
    }

    public override async IAsyncEnumerable<string> Akis(IReadOnlyList<SohbetMesaji> mesajlar, IstekSecenekleri s, [EnumeratorCancellation] CancellationToken ct)
    {
        using var istek = Istek(HttpMethod.Post, "/chat/completions", GovdeOlustur(mesajlar, s, _uyumlu));
        await foreach (var o in SseAkisi(istek, ct))
        {
            var (metin, hata, bitti) = DeltaCoz(o.Veri);
            if (hata != null) throw new SaglayiciHatasi($"{Ad}: {SaglayiciHatasi.AnahtarGizle(hata)}");
            if (!string.IsNullOrEmpty(metin)) yield return metin;
            if (bitti) yield break;
        }
    }

    public override async Task<List<string>> ModelleriGetir(CancellationToken ct)
    {
        using var istek = Istek(HttpMethod.Get, "/models");
        using var y = await Gonder(istek, ct);
        var n = JsonNode.Parse(await y.Content.ReadAsStringAsync(ct));
        return n?["data"]?.AsArray().Select(x => x?["id"]?.GetValue<string>()).OfType<string>().ToList() ?? new();
    }

    public override async Task<UretilenGorsel> GorselUret(string istem, string model, string boyut, CancellationToken ct)
    {
        if (_uyumlu) return await base.GorselUret(istem, model, boyut, ct);
        var g = new JsonObject { ["model"] = model, ["prompt"] = istem, ["size"] = boyut, ["n"] = 1 };
        if (model.StartsWith("dall-e", StringComparison.OrdinalIgnoreCase)) g["response_format"] = "b64_json";
        using var istek = Istek(HttpMethod.Post, "/images/generations", g);
        using var y = await Gonder(istek, ct);
        var n = JsonNode.Parse(await y.Content.ReadAsStringAsync(ct));
        var d = n?["data"]?.AsArray().FirstOrDefault();
        var b64 = d?["b64_json"]?.GetValue<string>();
        if (b64 == null)
        {
            var url = d?["url"]?.GetValue<string>() ?? throw new SaglayiciHatasi("OpenAI görsel döndürmedi.");
            return new UretilenGorsel(await Http.GetByteArrayAsync(url, ct), "image/png", d?["revised_prompt"]?.GetValue<string>());
        }
        return new UretilenGorsel(Convert.FromBase64String(b64), "image/png", d?["revised_prompt"]?.GetValue<string>());
    }

    /// <summary>Ses kaydını yazıya çevirir (OpenAI transcription).</summary>
    public async Task<string> SesiYaziyaCevir(byte[] ses, string dosyaAdi, string model, string? dil, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        var dosya = new ByteArrayContent(ses);
        dosya.Headers.ContentType = new MediaTypeHeaderValue(dosyaAdi.EndsWith(".m4a") ? "audio/mp4" : "application/octet-stream");
        form.Add(dosya, "file", dosyaAdi);
        form.Add(new StringContent(model), "model");
        if (!string.IsNullOrEmpty(dil)) form.Add(new StringContent(dil), "language");
        using var istek = Istek(HttpMethod.Post, "/audio/transcriptions");
        istek.Content = form;
        using var y = await Gonder(istek, ct);
        return JsonNode.Parse(await y.Content.ReadAsStringAsync(ct))?["text"]?.GetValue<string>() ?? "";
    }

    /// <summary>Metni sese çevirir (mp3).</summary>
    public async Task<byte[]> YaziyiSeseCevir(string metin, string model, string ses, CancellationToken ct)
    {
        var g = new JsonObject { ["model"] = model, ["voice"] = ses, ["input"] = Ortak.S.Kisalt(metin, 4000), ["response_format"] = "mp3" };
        using var istek = Istek(HttpMethod.Post, "/audio/speech", g);
        using var y = await Gonder(istek, ct);
        return await y.Content.ReadAsByteArrayAsync(ct);
    }
}
