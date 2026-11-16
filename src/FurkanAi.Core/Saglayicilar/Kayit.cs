namespace FurkanAi.Core.Saglayicilar;

public enum SaglayiciTuru { OpenAI, Gemini, Claude, OpenAIUyumlu }

[Flags]
public enum Yetenek
{
    Yok = 0,
    Metin = 1,
    GorselAnaliz = 2,     // görsel girdi (vision)
    Pdf = 4,              // PDF'i doğrudan okuyabilir
    GorselUretim = 8,
    SesYaziya = 16,
    YaziSese = 32
}

public sealed record SaglayiciTanimi(SaglayiciTuru Tur, string Ad, string VarsayilanTabanUrl, bool AnahtarGerekli, string AnahtarSayfasi, string Aciklama);

public sealed record ModelBilgisi(SaglayiciTuru Saglayici, string Id, string Ad, Yetenek Yetenekler, bool Varsayilan = false);

/// <summary>
/// Merkezi model kaydı. Buradaki liste yalnızca başlangıç önerisidir; sağlayıcılar modelleri sık değiştirir.
/// Kullanıcı "Modelleri getir" ile güncel listeyi alabilir veya model adını elle yazabilir.
/// </summary>
public static class ModelKaydi
{
    public static readonly IReadOnlyList<SaglayiciTanimi> Saglayicilar = new SaglayiciTanimi[]
    {
        new(SaglayiciTuru.OpenAI, "OpenAI", "https://api.openai.com/v1", true, "https://platform.openai.com/api-keys", "GPT modelleri, görsel üretim, ses"),
        new(SaglayiciTuru.Gemini, "Google Gemini", "https://generativelanguage.googleapis.com/v1beta", true, "https://aistudio.google.com/apikey", "Gemini modelleri, görsel analiz ve üretim"),
        new(SaglayiciTuru.Claude, "Anthropic Claude", "https://api.anthropic.com/v1", true, "https://console.anthropic.com/settings/keys", "Claude modelleri, görsel ve PDF analizi"),
        new(SaglayiciTuru.OpenAIUyumlu, "OpenAI uyumlu", "http://192.168.1.10:11434/v1", false, "", "Ollama, LM Studio, OpenRouter, Groq vb. (kendi adresiniz)")
    };

    public static readonly IReadOnlyList<ModelBilgisi> Modeller = new ModelBilgisi[]
    {
        new(SaglayiciTuru.OpenAI, "gpt-5-mini", "GPT-5 mini", Yetenek.Metin | Yetenek.GorselAnaliz | Yetenek.Pdf, true),
        new(SaglayiciTuru.OpenAI, "gpt-5", "GPT-5", Yetenek.Metin | Yetenek.GorselAnaliz | Yetenek.Pdf),
        new(SaglayiciTuru.OpenAI, "gpt-4.1-mini", "GPT-4.1 mini", Yetenek.Metin | Yetenek.GorselAnaliz | Yetenek.Pdf),
        new(SaglayiciTuru.OpenAI, "gpt-image-1", "GPT Image 1", Yetenek.GorselUretim),
        new(SaglayiciTuru.OpenAI, "gpt-4o-mini-transcribe", "Ses → yazı", Yetenek.SesYaziya),
        new(SaglayiciTuru.OpenAI, "gpt-4o-mini-tts", "Yazı → ses", Yetenek.YaziSese),

        new(SaglayiciTuru.Gemini, "gemini-2.5-flash", "Gemini 2.5 Flash", Yetenek.Metin | Yetenek.GorselAnaliz | Yetenek.Pdf, true),
        new(SaglayiciTuru.Gemini, "gemini-2.5-pro", "Gemini 2.5 Pro", Yetenek.Metin | Yetenek.GorselAnaliz | Yetenek.Pdf),
        new(SaglayiciTuru.Gemini, "gemini-2.5-flash-lite", "Gemini 2.5 Flash-Lite", Yetenek.Metin | Yetenek.GorselAnaliz | Yetenek.Pdf),
        new(SaglayiciTuru.Gemini, "gemini-2.5-flash-image", "Gemini 2.5 Flash Image", Yetenek.GorselUretim),

        new(SaglayiciTuru.Claude, "claude-sonnet-5-5", "Claude Sonnet 5.5", Yetenek.Metin | Yetenek.GorselAnaliz | Yetenek.Pdf, true),
        new(SaglayiciTuru.Claude, "claude-opus-5-5", "Claude Opus 5.5", Yetenek.Metin | Yetenek.GorselAnaliz | Yetenek.Pdf),
        new(SaglayiciTuru.Claude, "claude-haiku-5-5", "Claude Haiku 5.5", Yetenek.Metin | Yetenek.GorselAnaliz | Yetenek.Pdf),

        new(SaglayiciTuru.OpenAIUyumlu, "llama3.2", "Llama 3.2 (Ollama örneği)", Yetenek.Metin, true)
    };

    public static SaglayiciTanimi Tanim(SaglayiciTuru t) => Saglayicilar.First(s => s.Tur == t);

    public static IEnumerable<ModelBilgisi> SohbetModelleri(SaglayiciTuru t) => Modeller.Where(m => m.Saglayici == t && m.Yetenekler.HasFlag(Yetenek.Metin));

    public static IEnumerable<ModelBilgisi> GorselModelleri(SaglayiciTuru t) => Modeller.Where(m => m.Saglayici == t && m.Yetenekler.HasFlag(Yetenek.GorselUretim));

    public static string VarsayilanModel(SaglayiciTuru t) =>
        Modeller.FirstOrDefault(m => m.Saglayici == t && m.Varsayilan)?.Id ?? SohbetModelleri(t).FirstOrDefault()?.Id ?? "";

    /// <summary>
    /// Bilinmeyen (elle yazılmış/getirilmiş) bir modelin yeteneklerini adından tahmin eder.
    /// Tahmin yanlışsa sağlayıcı hata döndürür ve kullanıcıya açıkça gösterilir.
    /// </summary>
    public static Yetenek YetenekTahmin(SaglayiciTuru t, string model)
    {
        var kayitli = Modeller.FirstOrDefault(m => m.Saglayici == t && string.Equals(m.Id, model, StringComparison.OrdinalIgnoreCase));
        if (kayitli != null) return kayitli.Yetenekler;
        var m = model.ToLowerInvariant();
        if (m.Contains("image") || m.StartsWith("dall-e")) return Yetenek.GorselUretim;
        if (m.Contains("embed") || m.Contains("tts") || m.Contains("transcribe") || m.Contains("whisper") || m.Contains("moderation")) return Yetenek.Yok;
        return t switch
        {
            SaglayiciTuru.Claude => Yetenek.Metin | Yetenek.GorselAnaliz | Yetenek.Pdf,
            SaglayiciTuru.Gemini => Yetenek.Metin | Yetenek.GorselAnaliz | Yetenek.Pdf,
            SaglayiciTuru.OpenAI => Yetenek.Metin | (m.StartsWith("gpt-4o") || m.StartsWith("gpt-4.1") || m.StartsWith("gpt-5") || m.StartsWith("o3") || m.StartsWith("o4") ? Yetenek.GorselAnaliz | Yetenek.Pdf : Yetenek.Yok),
            _ => Yetenek.Metin | (m.Contains("vision") || m.Contains("llava") || m.Contains("-vl") || m.Contains("gemma3") ? Yetenek.GorselAnaliz : Yetenek.Yok)
        };
    }

    /// <summary>"Modelleri getir" sonucundan sohbete uygun olmayanları ayıklar.</summary>
    public static List<string> SohbeteUygun(SaglayiciTuru t, IEnumerable<string> idler) =>
        idler.Where(i => YetenekTahmin(t, i).HasFlag(Yetenek.Metin)).Distinct().OrderBy(i => i, StringComparer.OrdinalIgnoreCase).ToList();
}
