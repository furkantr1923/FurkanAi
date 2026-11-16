using System.Text;
using System.Text.Json.Nodes;
using FurkanAi.Core.Saglayicilar;

namespace FurkanAi.Tests;

public class SaglayiciTestleri
{
    private static readonly IstekSecenekleri Secenek = new() { Model = "test-model", SistemIstemi = "Sen yardımcısın.", AzamiToken = 512 };
    private static readonly EkIcerik Gorsel = new("a.png", "image/png", new byte[] { 1, 2, 3 });
    private static readonly EkIcerik Pdf = new("b.pdf", "application/pdf", Encoding.ASCII.GetBytes("%PDF-1.4"), "pdf metni");
    private static readonly EkIcerik Txt = new("c.txt", "text/plain", Encoding.UTF8.GetBytes("merhaba"), "merhaba");

    private static List<SohbetMesaji> Mesajlar() => new()
    {
        new(Rol.Kullanici, "Bunu incele", new[] { Gorsel, Pdf, Txt }),
        new(Rol.Asistan, "Tamam"),
        new(Rol.Kullanici, "Devam")
    };

    [Fact]
    public async Task Sse_cok_satirli_veri_ve_olay_adlarini_okur()
    {
        var ham = ": canlı\nevent: ping\ndata: {\"a\":1}\n\ndata: satir1\ndata: satir2\n\ndata: [DONE]\n";
        var liste = new List<SseOlayi>();
        await foreach (var o in SseOkuyucu.Oku(new StringReader(ham))) liste.Add(o);
        Assert.Equal(3, liste.Count);
        Assert.Equal("ping", liste[0].Olay);
        Assert.Equal("satir1\nsatir2", liste[1].Veri);
        Assert.Equal("[DONE]", liste[2].Veri);
    }

    [Fact]
    public void OpenAI_govdesi_sistem_gorsel_pdf_ve_metin_parcalarini_icerir()
    {
        var g = OpenAiIstemcisi.GovdeOlustur(Mesajlar(), Secenek, uyumlu: false);
        var msj = g["messages"]!.AsArray();
        Assert.Equal("system", msj[0]!["role"]!.GetValue<string>());
        var parca = msj[1]!["content"]!.AsArray();
        Assert.Contains(parca, p => p!["type"]!.GetValue<string>() == "image_url" && p["image_url"]!["url"]!.GetValue<string>().StartsWith("data:image/png;base64,"));
        Assert.Contains(parca, p => p!["type"]!.GetValue<string>() == "file");
        Assert.Contains(parca, p => p!["type"]!.GetValue<string>() == "text" && p["text"]!.GetValue<string>().Contains("c.txt"));
        Assert.True(g["stream"]!.GetValue<bool>());
        Assert.Null(g["temperature"]);
    }

    [Fact]
    public void OpenAI_uyumlu_sunucuya_pdf_metin_olarak_gider()
    {
        var parca = OpenAiIstemcisi.GovdeOlustur(Mesajlar(), Secenek, uyumlu: true)["messages"]!.AsArray()[1]!["content"]!.AsArray();
        Assert.DoesNotContain(parca, p => p!["type"]!.GetValue<string>() == "file");
        Assert.Contains(parca, p => p!["type"]!.GetValue<string>() == "text" && p["text"]!.GetValue<string>().Contains("pdf metni"));
    }

    [Theory]
    [InlineData("{\"choices\":[{\"delta\":{\"content\":\"Mer\"}}]}", "Mer", false)]
    [InlineData("[DONE]", null, true)]
    [InlineData("{\"choices\":[{\"delta\":{}}]}", null, false)]
    public void OpenAI_delta_cozumu(string veri, string? beklenen, bool bitti)
    {
        var (m, h, b) = OpenAiIstemcisi.DeltaCoz(veri);
        Assert.Equal(beklenen, m); Assert.Null(h); Assert.Equal(bitti, b);
    }

    [Fact]
    public void Gemini_govdesi_rolleri_ve_sistem_talimatini_dogru_kurar()
    {
        var g = GeminiIstemcisi.GovdeOlustur(Mesajlar(), Secenek);
        var c = g["contents"]!.AsArray();
        Assert.Equal(new[] { "user", "model", "user" }, c.Select(x => x!["role"]!.GetValue<string>()));
        Assert.Equal(2, c[0]!["parts"]!.AsArray().Count(p => p!["inline_data"] != null));
        Assert.Equal("Sen yardımcısın.", g["systemInstruction"]!["parts"]![0]!["text"]!.GetValue<string>());
        Assert.Equal(512, g["generationConfig"]!["maxOutputTokens"]!.GetValue<int>());
        Assert.Equal("/models/gemini-2.5-flash", GeminiIstemcisi.ModelYolu("models/gemini-2.5-flash"));
    }

    [Fact]
    public void Gemini_delta_ve_guvenlik_engeli()
    {
        Assert.Equal("Selam", GeminiIstemcisi.DeltaCoz("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Sel\"},{\"text\":\"am\"}]}}]}").Metin);
        Assert.NotNull(GeminiIstemcisi.DeltaCoz("{\"promptFeedback\":{\"blockReason\":\"SAFETY\"}}").Hata);
    }

    [Fact]
    public void Claude_govdesi_ardisik_rolleri_birlestirir_ve_kullaniciyla_baslar()
    {
        var m = new List<SohbetMesaji>
        {
            new(Rol.Asistan, "Merhaba, nasıl yardımcı olabilirim?"),
            new(Rol.Kullanici, "Bir"), new(Rol.Kullanici, "İki", new[] { Gorsel, Pdf })
        };
        var g = ClaudeIstemcisi.GovdeOlustur(m, Secenek);
        var msj = g["messages"]!.AsArray();
        Assert.Single(msj);
        Assert.Equal("user", msj[0]!["role"]!.GetValue<string>());
        var bloklar = msj[0]!["content"]!.AsArray();
        Assert.Contains(bloklar, b => b!["type"]!.GetValue<string>() == "image");
        Assert.Contains(bloklar, b => b!["type"]!.GetValue<string>() == "document");
        Assert.Equal("Sen yardımcısın.", g["system"]!.GetValue<string>());
        Assert.Equal(512, g["max_tokens"]!.GetValue<int>());
    }

    [Fact]
    public void Claude_delta_olaylari()
    {
        Assert.Equal("Hi", ClaudeIstemcisi.DeltaCoz("content_block_delta", "{\"type\":\"content_block_delta\",\"index\":0,\"delta\":{\"type\":\"text_delta\",\"text\":\"Hi\"}}").Metin);
        Assert.True(ClaudeIstemcisi.DeltaCoz("message_stop", "{\"type\":\"message_stop\"}").Bitti);
        Assert.Equal("Overloaded", ClaudeIstemcisi.DeltaCoz("error", "{\"type\":\"error\",\"error\":{\"type\":\"overloaded_error\",\"message\":\"Overloaded\"}}").Hata);
        Assert.Null(ClaudeIstemcisi.DeltaCoz("ping", "{\"type\":\"ping\"}").Metin);
    }

    [Fact]
    public void Hata_mesajlari_turkce_ve_anahtarsiz()
    {
        var h = SaglayiciHatasi.Olustur("OpenAI", 401, "Incorrect API key provided: sk-proj-abcdefghijklmnopqrstuvwxyz123456");
        Assert.Contains("API anahtarı geçersiz", h.Message);
        Assert.DoesNotContain("abcdefghijklmnopqrstuvwxyz123456", h.Message);
        Assert.True(SaglayiciHatasi.Olustur("Claude", 429, null).TekrarDenenebilir);
        Assert.Equal("Bad", SaglayiciIstemcisi.HataMetni("{\"error\":{\"message\":\"Bad\"}}"));
        Assert.Equal("Gem", SaglayiciIstemcisi.HataMetni("[{\"error\":{\"code\":400,\"message\":\"Gem\"}}]"));
    }

    [Fact]
    public void Anahtar_gerekli_saglayici_anahtarsiz_olusturulamaz_uyumlu_olusturulabilir()
    {
        using var http = new HttpClient();
        Assert.Throws<SaglayiciHatasi>(() => IstemciFabrikasi.Olustur(SaglayiciTuru.Claude, http, null, null));
        var u = IstemciFabrikasi.Olustur(SaglayiciTuru.OpenAIUyumlu, http, null, "http://localhost:11434/v1/");
        Assert.Equal(SaglayiciTuru.OpenAIUyumlu, u.Tur);
    }

    [Fact]
    public void Model_kaydi_varsayilanlar_ve_tahmin()
    {
        foreach (var t in Enum.GetValues<SaglayiciTuru>()) Assert.False(string.IsNullOrEmpty(ModelKaydi.VarsayilanModel(t)));
        Assert.True(ModelKaydi.YetenekTahmin(SaglayiciTuru.OpenAI, "gpt-4o").HasFlag(Yetenek.GorselAnaliz));
        Assert.Equal(Yetenek.GorselUretim, ModelKaydi.YetenekTahmin(SaglayiciTuru.OpenAI, "dall-e-3"));
        Assert.DoesNotContain("text-embedding-3-small", ModelKaydi.SohbeteUygun(SaglayiciTuru.OpenAI, new[] { "gpt-4o", "text-embedding-3-small", "whisper-1" }));
    }
}
