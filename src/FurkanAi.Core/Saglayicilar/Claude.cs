using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace FurkanAi.Core.Saglayicilar;

/// <summary>Anthropic Messages API (akışlı). Görsel ve PDF blokları doğrudan desteklenir.</summary>
public sealed class ClaudeIstemcisi : SaglayiciIstemcisi
{
    public const string ApiSurumu = "2023-06-01";
    private static readonly HashSet<string> GorselTurleri = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/gif", "image/webp" };

    public ClaudeIstemcisi(HttpClient http, string? anahtar, string? tabanUrl) : base(SaglayiciTuru.Claude, http, anahtar, tabanUrl) { }

    protected override void Kimlik(HttpRequestMessage i)
    {
        i.Headers.TryAddWithoutValidation("x-api-key", Anahtar);
        i.Headers.TryAddWithoutValidation("anthropic-version", ApiSurumu);
    }

    internal static JsonObject GovdeOlustur(IReadOnlyList<SohbetMesaji> mesajlar, IstekSecenekleri s)
    {
        var dizi = new JsonArray();
        var sistem = new List<string>();
        if (!string.IsNullOrWhiteSpace(s.SistemIstemi)) sistem.Add(s.SistemIstemi);
        foreach (var m in mesajlar)
        {
            if (m.Rol == Rol.Sistem) { sistem.Add(m.Metin); continue; }
            var bloklar = new JsonArray();
            if (m.Rol == Rol.Kullanici)
                foreach (var e in m.EkListesi)
                {
                    if (e.Tur == EkTuru.Gorsel && GorselTurleri.Contains(e.Mime))
                        bloklar.Add(new JsonObject { ["type"] = "image", ["source"] = new JsonObject { ["type"] = "base64", ["media_type"] = e.Mime.ToLowerInvariant(), ["data"] = Convert.ToBase64String(e.Veri) } });
                    else if (e.Tur == EkTuru.Pdf)
                        bloklar.Add(new JsonObject { ["type"] = "document", ["source"] = new JsonObject { ["type"] = "base64", ["media_type"] = "application/pdf", ["data"] = Convert.ToBase64String(e.Veri) } });
                    else if (!string.IsNullOrEmpty(e.Metin))
                        bloklar.Add(new JsonObject { ["type"] = "text", ["text"] = MetinEki(e) });
                }
            if (!string.IsNullOrWhiteSpace(m.Metin)) bloklar.Add(new JsonObject { ["type"] = "text", ["text"] = m.Metin });
            if (bloklar.Count == 0) continue;
            var rol = m.Rol == Rol.Asistan ? "assistant" : "user";
            // Messages API rollerin sırayla gelmesini bekler: art arda aynı rol birleştirilir
            if (dizi.Count > 0 && dizi[^1]!["role"]!.GetValue<string>() == rol)
                foreach (var b in bloklar.ToList()) { bloklar.Remove(b); dizi[^1]!["content"]!.AsArray().Add(b); }
            else dizi.Add(new JsonObject { ["role"] = rol, ["content"] = bloklar });
        }
        // İlk mesaj kullanıcıdan olmalı
        while (dizi.Count > 0 && dizi[0]!["role"]!.GetValue<string>() != "user") dizi.RemoveAt(0);
        var g = new JsonObject { ["model"] = s.Model, ["max_tokens"] = s.AzamiToken, ["messages"] = dizi, ["stream"] = true };
        if (sistem.Count > 0) g["system"] = string.Join("\n\n", sistem);
        if (s.Sicaklik is double t) g["temperature"] = Math.Clamp(t, 0, 1);
        return g;
    }

    internal static (string? Metin, string? Hata, bool Bitti) DeltaCoz(string? olay, string veri)
    {
        var n = JsonNode.Parse(veri);
        var tur = n?["type"]?.GetValue<string>() ?? olay;
        return tur switch
        {
            "content_block_delta" when n?["delta"]?["type"]?.GetValue<string>() == "text_delta" => (n["delta"]!["text"]?.GetValue<string>(), null, false),
            "error" => (null, n?["error"]?["message"]?.GetValue<string>() ?? "Bilinmeyen hata", true),
            "message_stop" => (null, null, true),
            _ => (null, null, false)
        };
    }

    public override async IAsyncEnumerable<string> Akis(IReadOnlyList<SohbetMesaji> mesajlar, IstekSecenekleri s, [EnumeratorCancellation] CancellationToken ct)
    {
        using var istek = Istek(HttpMethod.Post, "/messages", GovdeOlustur(mesajlar, s));
        await foreach (var o in SseAkisi(istek, ct))
        {
            var (metin, hata, bitti) = DeltaCoz(o.Olay, o.Veri);
            if (hata != null) throw new SaglayiciHatasi($"Claude: {SaglayiciHatasi.AnahtarGizle(hata)}", null, hata.Contains("overloaded", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(metin)) yield return metin;
            if (bitti) yield break;
        }
    }

    public override async Task<List<string>> ModelleriGetir(CancellationToken ct)
    {
        using var istek = Istek(HttpMethod.Get, "/models?limit=100");
        using var y = await Gonder(istek, ct);
        var n = JsonNode.Parse(await y.Content.ReadAsStringAsync(ct));
        return n?["data"]?.AsArray().Select(x => x?["id"]?.GetValue<string>()).OfType<string>().ToList() ?? new();
    }
}
