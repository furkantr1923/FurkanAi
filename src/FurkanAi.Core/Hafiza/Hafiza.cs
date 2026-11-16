using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FurkanAi.Core.Hafiza;

/// <summary>
/// Denetimli hafıza: yalnızca kullanıcının onayladığı kısa bilgiler saklanır ve sistem istemine eklenir.
/// Sohbet geçmişi hafıza DEĞİLDİR; model kendi kendine hafızaya bir şey yazamaz.
/// </summary>
public static partial class HafizaYardimcisi
{
    public const int AzamiOge = 50;
    public const int AzamiUzunluk = 300;

    public static string? Dogrula(string? metin)
    {
        if (string.IsNullOrWhiteSpace(metin)) return "Boş bilgi kaydedilemez.";
        if (metin.Trim().Length > AzamiUzunluk) return $"Hafıza bilgisi en fazla {AzamiUzunluk} karakter olabilir.";
        if (AnahtarBenzeriRx().IsMatch(metin)) return "Bu bilgi bir parola/API anahtarı gibi görünüyor. Güvenliğiniz için hafızaya kaydedilmez.";
        return null;
    }

    /// <summary>Etkin hafıza öğelerini sistem istemine eklenecek bloğa çevirir.</summary>
    public static string? SistemBlogu(IEnumerable<string> ogeler)
    {
        var l = ogeler.Where(o => !string.IsNullOrWhiteSpace(o)).Take(AzamiOge).ToList();
        if (l.Count == 0) return null;
        var sb = new StringBuilder("Kullanıcının onayladığı ve hatırlamanı istediği bilgiler (yalnızca ilgili olduğunda kullan, her yanıtta tekrar etme):\n");
        foreach (var o in l) sb.Append("- ").Append(o.Trim()).Append('\n');
        return sb.ToString().TrimEnd();
    }

    /// <summary>Modelden hafıza ÖNERİSİ istemek için kullanılan istem. Öneriler kullanıcı onayı olmadan kaydedilmez.</summary>
    public const string OneriIstemi =
        "Aşağıdaki sohbetten, kullanıcının gelecekteki sohbetlerde hatırlanmasını isteyebileceği kalıcı ve kişisel olmayan-hassas " +
        "bilgileri (tercihler, meslek, projeler, üslup istekleri) çıkar. Sağlık, finans, parola, kimlik numarası gibi hassas bilgileri ASLA önerme. " +
        "Her öğe kısa tek bir cümle olsun. Yalnızca JSON dizi döndür, ör: [\"Kısa yanıtları tercih ediyor\"]. Uygun bilgi yoksa [] döndür.";

    /// <summary>Model yanıtındaki JSON diziyi (kod bloğu içinde olsa bile) okur; geçersizse boş liste.</summary>
    public static List<string> OnerileriCoz(string yanit)
    {
        var m = DiziRx().Match(yanit);
        if (!m.Success) return new();
        try
        {
            return (JsonSerializer.Deserialize<List<string>>(m.Value) ?? new())
                .Select(x => x.Trim()).Where(x => Dogrula(x) == null).Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToList();
        }
        catch (JsonException) { return new(); }
    }

    [GeneratedRegex(@"\[\s*(?:""(?:\\.|[^""\\])*""\s*,?\s*)*\]", RegexOptions.Singleline)] private static partial Regex DiziRx();
    [GeneratedRegex(@"\b(sk-[A-Za-z0-9_\-]{16,}|AIza[0-9A-Za-z_\-]{20,}|sk-ant-[A-Za-z0-9_\-]{16,}|gh[pousr]_[A-Za-z0-9]{20,})|\b(şifre|parola|password)\s*[:=]", RegexOptions.IgnoreCase)] private static partial Regex AnahtarBenzeriRx();
}
