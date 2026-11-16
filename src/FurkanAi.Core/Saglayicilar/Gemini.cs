using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace FurkanAi.Core.Saglayicilar;

/// <summary>Google Gemini (Generative Language API, v1beta). Anahtar x-goog-api-key başlığıyla gönderilir (URL'ye yazılmaz).</summary>
public sealed class GeminiIstemcisi : SaglayiciIstemcisi
{
    public GeminiIstemcisi(HttpClient http, string? anahtar, string? tabanUrl) : base(SaglayiciTuru.Gemini, http, anahtar, tabanUrl) { }

    protected override void Kimlik(HttpRequestMessage i) => i.Headers.TryAddWithoutValidation("x-goog-api-key", Anahtar);

    internal static string ModelYolu(string model) => "/models/" + Uri.EscapeDataString(model.StartsWith("models/", StringComparison.Ordinal) ? model[7..] : model);

    internal static JsonObject GovdeOlustur(IReadOnlyList<SohbetMesaji> mesajlar, IstekSecenekleri s)
    {
        var icerik = new JsonArray();
        var sistem = new List<string>();
        if (!string.IsNullOrWhiteSpace(s.SistemIstemi)) sistem.Add(s.SistemIstemi);
        foreach (var m in mesajlar)
        {
            if (m.Rol == Rol.Sistem) { sistem.Add(m.Metin); continue; }
            var parcalar = new JsonArray();
            if (m.Rol == Rol.Kullanici)
                foreach (var e in m.EkListesi)
                {
                    if (e.Tur is EkTuru.Gorsel or EkTuru.Pdf)
                        parcalar.Add(new JsonObject { ["inline_data"] = new JsonObject { ["mime_type"] = e.Mime, ["data"] = Convert.ToBase64String(e.Veri) } });
                    else if (!string.IsNullOrEmpty(e.Metin))
                        parcalar.Add(new JsonObject { ["text"] = MetinEki(e) });
                }
            if (!string.IsNullOrEmpty(m.Metin) || parcalar.Count == 0) parcalar.Add(new JsonObject { ["text"] = m.Metin });
            var rol = m.Rol == Rol.Asistan ? "model" : "user";
            // Gemini art arda aynı rolü kabul etse de birleştirmek daha tutarlı sonuç verir
            if (icerik.Count > 0 && icerik[^1]!["role"]!.GetValue<string>() == rol)
                foreach (var p in parcalar.ToList()) { parcalar.Remove(p); icerik[^1]!["parts"]!.AsArray().Add(p); }
            else icerik.Add(new JsonObject { ["role"] = rol, ["parts"] = parcalar });
        }
        var g = new JsonObject { ["contents"] = icerik };
        if (sistem.Count > 0) g["systemInstruction"] = new JsonObject { ["parts"] = new JsonArray(new JsonObject { ["text"] = string.Join("\n\n", sistem) }) };
        var ayar = new JsonObject { ["maxOutputTokens"] = s.AzamiToken };
        if (s.Sicaklik is double t) ayar["temperature"] = t;
        g["generationConfig"] = ayar;
        return g;
    }

    internal static (string? Metin, string? Hata, bool Bitti) DeltaCoz(string veri)
    {
        var n = JsonNode.Parse(veri);
        if (n?["error"] is JsonNode h) return (null, h["message"]?.GetValue<string>() ?? h.ToJsonString(), true);
        if (n?["promptFeedback"]?["blockReason"]?.GetValue<string>() is string engel)
            return (null, $"İstek güvenlik filtresine takıldı ({engel}).", true);
        var aday = n?["candidates"]?.AsArray().FirstOrDefault();
        var metin = string.Concat(aday?["content"]?["parts"]?.AsArray().Select(p => p?["thought"]?.GetValue<bool>() == true ? null : p?["text"]?.GetValue<string>()) ?? Array.Empty<string?>());
        var neden = aday?["finishReason"]?.GetValue<string>();
        if (neden is "SAFETY" or "PROHIBITED_CONTENT" or "BLOCKLIST" && metin.Length == 0) return (null, "Yanıt güvenlik filtresine takıldı.", true);
        return (metin.Length > 0 ? metin : null, null, false);
    }

    public override async IAsyncEnumerable<string> Akis(IReadOnlyList<SohbetMesaji> mesajlar, IstekSecenekleri s, [EnumeratorCancellation] CancellationToken ct)
    {
        using var istek = Istek(HttpMethod.Post, ModelYolu(s.Model) + ":streamGenerateContent?alt=sse", GovdeOlustur(mesajlar, s));
        await foreach (var o in SseAkisi(istek, ct))
        {
            var (metin, hata, bitti) = DeltaCoz(o.Veri);
            if (hata != null) throw new SaglayiciHatasi($"Gemini: {SaglayiciHatasi.AnahtarGizle(hata)}");
            if (metin != null) yield return metin;
            if (bitti) yield break;
        }
    }

    public override async Task<List<string>> ModelleriGetir(CancellationToken ct)
    {
        using var istek = Istek(HttpMethod.Get, "/models?pageSize=200");
        using var y = await Gonder(istek, ct);
        var n = JsonNode.Parse(await y.Content.ReadAsStringAsync(ct));
        return n?["models"]?.AsArray()
            .Where(m => m?["supportedGenerationMethods"]?.AsArray().Any(x => x?.GetValue<string>() == "generateContent") == true)
            .Select(m => m?["name"]?.GetValue<string>()?.Replace("models/", "")).OfType<string>().ToList() ?? new();
    }

    public override async Task<UretilenGorsel> GorselUret(string istem, string model, string boyut, CancellationToken ct)
    {
        var g = new JsonObject
        {
            ["contents"] = new JsonArray(new JsonObject { ["role"] = "user", ["parts"] = new JsonArray(new JsonObject { ["text"] = istem }) }),
            ["generationConfig"] = new JsonObject { ["responseModalities"] = new JsonArray("TEXT", "IMAGE") }
        };
        using var istek = Istek(HttpMethod.Post, ModelYolu(model) + ":generateContent", g);
        using var y = await Gonder(istek, ct);
        var n = JsonNode.Parse(await y.Content.ReadAsStringAsync(ct));
        string? aciklama = null;
        foreach (var p in n?["candidates"]?.AsArray().FirstOrDefault()?["content"]?["parts"]?.AsArray() ?? new JsonArray())
        {
            var veri = p?["inlineData"] ?? p?["inline_data"];
            if (veri != null)
                return new UretilenGorsel(Convert.FromBase64String(veri["data"]!.GetValue<string>()), (veri["mimeType"] ?? veri["mime_type"])?.GetValue<string>() ?? "image/png", aciklama);
            aciklama ??= p?["text"]?.GetValue<string>();
        }
        throw new SaglayiciHatasi("Gemini görsel döndürmedi." + (aciklama != null ? " Yanıt: " + Ortak.S.Kisalt(aciklama, 200) : " İstemi değiştirip tekrar deneyin."));
    }
}
