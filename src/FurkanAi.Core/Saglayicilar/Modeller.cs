namespace FurkanAi.Core.Saglayicilar;

public enum Rol { Sistem, Kullanici, Asistan }

public enum EkTuru { Gorsel, Pdf, Metin, Docx, Desteklenmeyen }

/// <summary>Mesaja eklenen dosya. <see cref="Metin"/> TXT/DOCX/PDF'ten çıkarılan düz metindir (varsa).</summary>
public sealed record EkIcerik(string Ad, string Mime, byte[] Veri, string? Metin = null)
{
    public EkTuru Tur => Dosyalar.DosyaOkuyucu.TurBul(Mime, Ad);
}

public sealed record SohbetMesaji(Rol Rol, string Metin, IReadOnlyList<EkIcerik>? Ekler = null)
{
    public IReadOnlyList<EkIcerik> EkListesi => Ekler ?? Array.Empty<EkIcerik>();
}

public sealed class IstekSecenekleri
{
    public string Model { get; set; } = "";
    public string? SistemIstemi { get; set; }
    /// <summary>null: sağlayıcı varsayılanı (bazı akıl yürütme modelleri sıcaklık parametresini kabul etmez).</summary>
    public double? Sicaklik { get; set; }
    public int AzamiToken { get; set; } = 4096;
}

/// <summary>Sağlayıcıdan dönen anlaşılır hata. Mesaj kullanıcıya gösterilebilir (anahtar içermez).</summary>
public sealed class SaglayiciHatasi : Exception
{
    public int? HttpKodu { get; }
    public bool TekrarDenenebilir { get; }
    public SaglayiciHatasi(string mesaj, int? kod = null, bool tekrar = false, Exception? ic = null) : base(mesaj, ic) { HttpKodu = kod; TekrarDenenebilir = tekrar; }

    /// <summary>HTTP durum kodunu ve sağlayıcının hata metnini Türkçe açıklamaya çevirir.</summary>
    public static SaglayiciHatasi Olustur(string saglayici, int kod, string? ayrinti)
    {
        ayrinti = Ortak.S.Kisalt(AnahtarGizle(ayrinti), 300);
        var (m, tekrar) = kod switch
        {
            400 => ($"{saglayici} isteği reddetti (geçersiz istek). Model bu içerik türünü desteklemiyor olabilir.", false),
            401 => ($"{saglayici} API anahtarı geçersiz veya süresi dolmuş. Sağlayıcılar ekranından anahtarı kontrol edin.", false),
            403 => ($"{saglayici} bu işleme izin vermedi (hesap/bölge/model erişimi).", false),
            404 => ($"{saglayici}: model veya adres bulunamadı. Model adını kontrol edin ya da 'Modelleri getir' kullanın.", false),
            408 => ($"{saglayici} zaman aşımına uğradı.", true),
            413 => ($"{saglayici}: gönderilen içerik çok büyük. Dosyayı küçültün veya sohbeti kısaltın.", false),
            429 => ($"{saglayici} kota veya hız sınırına ulaşıldı. Biraz bekleyip tekrar deneyin ya da hesabınızın kotasını kontrol edin.", true),
            529 => ($"{saglayici} şu an aşırı yoğun. Biraz sonra tekrar deneyin.", true),
            >= 500 => ($"{saglayici} sunucusunda geçici bir sorun var ({kod}).", true),
            _ => ($"{saglayici} hatası ({kod}).", false)
        };
        if (!string.IsNullOrWhiteSpace(ayrinti)) m += "\nAyrıntı: " + ayrinti;
        return new SaglayiciHatasi(m, kod, tekrar);
    }

    /// <summary>Hata metinlerinde yanlışlıkla geçebilecek anahtar benzeri dizileri maskeler.</summary>
    public static string? AnahtarGizle(string? s) => s == null ? null :
        System.Text.RegularExpressions.Regex.Replace(s, @"(sk-[A-Za-z0-9_\-]{6})[A-Za-z0-9_\-]{10,}|(AIza[0-9A-Za-z_\-]{4})[0-9A-Za-z_\-]{20,}|(sk-ant-[A-Za-z0-9_\-]{4})[A-Za-z0-9_\-]{10,}", m => (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Value) + "…");
}
