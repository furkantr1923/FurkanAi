using System.Text.RegularExpressions;
using FurkanAi.Core.Saglayicilar;
using FurkanAi.Mobile.Platforms.Android;

namespace FurkanAi.Mobile.Services;

/// <summary>Sesten yazıya (STT) ve yazıdan sese (TTS). İki yön birbirinden bağımsız ayarlanır.</summary>
public sealed partial class SesServisi
{
    private readonly Ayarlar _ayar;
    private readonly SaglayiciServisi _sag;
    private CancellationTokenSource? _okuma;
    public long? OkunanMesaj { get; private set; }
    public event Action? Degisti;

    public SesServisi(Ayarlar ayar, SaglayiciServisi sag) { _ayar = ayar; _sag = sag; }

    public bool Kayitta => AndroidSesKaydi.Kayitta;

    /// <summary>Cihaz motoru: sistem konuşma ekranını açar ve metni döndürür.</summary>
    public async Task<string?> CihazlaDinle()
    {
        if (!AndroidKonusma.Var()) throw new FeatureNotSupportedException("Bu cihazda konuşma tanıma servisi yok. Ayarlar › Ses'ten 'OpenAI' motorunu seçebilirsiniz.");
        return await AndroidKonusma.Dinle(_ayar.SesDili);
    }

    /// <summary>OpenAI motoru: mikrofon kaydını başlatır (izin yalnızca burada istenir).</summary>
    public async Task KayitBaslat()
    {
        if (await Permissions.RequestAsync<Permissions.Microphone>() != PermissionStatus.Granted)
            throw new PermissionException("Mikrofon izni verilmedi.");
        AndroidSesKaydi.Baslat();
        Degisti?.Invoke();
    }

    public async Task<string> KayitBitirVeCevir(CancellationToken ct = default)
    {
        var yol = AndroidSesKaydi.Durdur();
        Degisti?.Invoke();
        if (yol == null || !File.Exists(yol) || new FileInfo(yol).Length < 2000) throw new InvalidDataException("Kayıt çok kısa. Mikrofon düğmesine basıp konuşun, bitince tekrar basın.");
        try
        {
            if (await _sag.Istemci(SaglayiciTuru.OpenAI) is not OpenAiIstemcisi oi) throw new SaglayiciHatasi("OpenAI istemcisi oluşturulamadı.");
            var dil = _ayar.SesDili.Split('-')[0];
            return await oi.SesiYaziyaCevir(await File.ReadAllBytesAsync(yol, ct), Path.GetFileName(yol), "gpt-4o-mini-transcribe", dil, ct);
        }
        finally { try { File.Delete(yol); } catch { } }
    }

    public void KayitIptal() { AndroidSesKaydi.Durdur(sil: true); Degisti?.Invoke(); }

    /// <summary>Metni sesli okur. Markdown işaretleri ve kod blokları okunmaz.</summary>
    public async Task Oku(string metin, long? mesajId = null)
    {
        Sustur();
        var temiz = Temizle(metin);
        if (temiz.Length == 0) return;
        _okuma = new CancellationTokenSource();
        var ct = _okuma.Token;
        OkunanMesaj = mesajId; Degisti?.Invoke();
        try
        {
            if (_ayar.TtsMotoru == "openai")
            {
                if (await _sag.Istemci(SaglayiciTuru.OpenAI) is not OpenAiIstemcisi oi) return;
                var mp3 = await oi.YaziyiSeseCevir(temiz, "gpt-4o-mini-tts", _ayar.OpenAiSes, ct);
                await AndroidSesCalar.Cal(mp3, ct);
            }
            else
            {
                var yereller = await TextToSpeech.Default.GetLocalesAsync();
                var dil = _ayar.SesDili;
                var yerel = yereller.FirstOrDefault(l => $"{l.Language}-{l.Country}".Equals(dil, StringComparison.OrdinalIgnoreCase))
                            ?? yereller.FirstOrDefault(l => l.Language.Equals(dil.Split('-')[0], StringComparison.OrdinalIgnoreCase));
                // Uzun metni cümle gruplarına bölerek okur (motorların karakter sınırı için)
                foreach (var parca in Parcala(temiz, 3500))
                {
                    ct.ThrowIfCancellationRequested();
                    await TextToSpeech.Default.SpeakAsync(parca, new SpeechOptions { Locale = yerel, Pitch = 1.0f, Volume = 1.0f }, ct);
                }
            }
        }
        catch (OperationCanceledException) { }
        finally { if (OkunanMesaj == mesajId) { OkunanMesaj = null; Degisti?.Invoke(); } }
    }

    public void Sustur()
    {
        _okuma?.Cancel(); _okuma = null;
        AndroidSesCalar.Durdur();
        if (OkunanMesaj != null) { OkunanMesaj = null; Degisti?.Invoke(); }
    }

    internal static string Temizle(string md)
    {
        var s = KodBlokRx().Replace(md, " (kod bloğu) ");
        s = BaglantiRx().Replace(s, "$1");
        s = IsaretRx().Replace(s, "");
        return BoslukRx().Replace(s, " ").Trim();
    }

    private static IEnumerable<string> Parcala(string s, int azami)
    {
        while (s.Length > azami)
        {
            var i = s.LastIndexOfAny(new[] { '.', '!', '?', '\n' }, azami);
            if (i < azami / 2) i = azami;
            yield return s[..(i + 1)];
            s = s[(i + 1)..].TrimStart();
        }
        if (s.Length > 0) yield return s;
    }

    [GeneratedRegex(@"```[\s\S]*?(```|$)")] private static partial Regex KodBlokRx();
    [GeneratedRegex(@"\[([^\]]+)\]\([^)]+\)")] private static partial Regex BaglantiRx();
    [GeneratedRegex(@"[#*_`>|~]+")] private static partial Regex IsaretRx();
    [GeneratedRegex(@"[ \t]+")] private static partial Regex BoslukRx();
}
