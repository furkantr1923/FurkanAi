using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace FurkanAi.Core.Metin;

/// <summary>
/// Model yanıtları için küçük, güvenli Markdown → HTML çevirici.
/// Tüm metin önce HTML-kaçışlanır; ham HTML asla geçirilmez. Bağlantılar yalnızca http/https/mailto olabilir.
/// Desteklenenler: başlıklar, kalın/italik/üstü çizili, satır içi kod, kod blokları (dil etiketi + kopyala düğmesi),
/// sıralı/sırasız listeler, alıntı, yatay çizgi, tablo, bağlantı.
/// </summary>
public static partial class MarkdownCizici
{
    public static string Html(string? md)
    {
        if (string.IsNullOrEmpty(md)) return "";
        var satirlar = md.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder(md.Length + 64);
        var paragraf = new List<string>();
        var i = 0;

        void ParagrafBitir()
        {
            if (paragraf.Count == 0) return;
            sb.Append("<p>").Append(string.Join("<br>", paragraf.Select(Satir))).Append("</p>");
            paragraf.Clear();
        }

        while (i < satirlar.Length)
        {
            var s = satirlar[i];
            var kirpik = s.TrimStart();

            // Kod bloğu ``` veya ~~~ (akış sırasında kapanmamış blok da gösterilir)
            if (kirpik.StartsWith("```") || kirpik.StartsWith("~~~"))
            {
                ParagrafBitir();
                var cit = kirpik[..3];
                var dil = kirpik[3..].Trim();
                var kod = new List<string>();
                i++;
                while (i < satirlar.Length && !satirlar[i].TrimStart().StartsWith(cit)) kod.Add(satirlar[i++]);
                i++;
                var dilKac = WebUtility.HtmlEncode(DilTemizle().Replace(dil, ""));
                sb.Append("<div class=\"kod\"><div class=\"kod-ust\"><span>").Append(dilKac.Length > 0 ? dilKac : "kod")
                  .Append("</span><button type=\"button\" class=\"kod-kopyala\">Kopyala</button></div><pre><code")
                  .Append(dilKac.Length > 0 ? $" class=\"dil-{dilKac}\"" : "").Append('>')
                  .Append(WebUtility.HtmlEncode(string.Join("\n", kod))).Append("</code></pre></div>");
                continue;
            }

            if (string.IsNullOrWhiteSpace(s)) { ParagrafBitir(); i++; continue; }

            var baslik = BaslikRx().Match(s);
            if (baslik.Success)
            {
                ParagrafBitir();
                var d = Math.Min(baslik.Groups[1].Length + 2, 6); // # → h3 (sohbet balonunda dev başlık olmasın)
                sb.Append($"<h{d}>").Append(Satir(baslik.Groups[2].Value.TrimEnd('#', ' '))).Append($"</h{d}>");
                i++; continue;
            }

            if (CizgiRx().IsMatch(s)) { ParagrafBitir(); sb.Append("<hr>"); i++; continue; }

            if (kirpik.StartsWith('>'))
            {
                ParagrafBitir();
                var alinti = new List<string>();
                while (i < satirlar.Length && satirlar[i].TrimStart().StartsWith('>')) alinti.Add(satirlar[i++].TrimStart()[1..].TrimStart());
                sb.Append("<blockquote>").Append(Html(string.Join("\n", alinti))).Append("</blockquote>");
                continue;
            }

            // Tablo: | a | b | ardından |---|---|
            if (kirpik.StartsWith('|') && i + 1 < satirlar.Length && TabloAyracRx().IsMatch(satirlar[i + 1].Trim()))
            {
                ParagrafBitir();
                var basliklar = Hucreler(s);
                i += 2;
                sb.Append("<div class=\"tablo\"><table><thead><tr>");
                foreach (var h in basliklar) sb.Append("<th>").Append(Satir(h)).Append("</th>");
                sb.Append("</tr></thead><tbody>");
                while (i < satirlar.Length && satirlar[i].TrimStart().StartsWith('|'))
                {
                    sb.Append("<tr>");
                    foreach (var h in Hucreler(satirlar[i++])) sb.Append("<td>").Append(Satir(h)).Append("</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</tbody></table></div>");
                continue;
            }

            var liste = ListeRx().Match(s);
            if (liste.Success)
            {
                ParagrafBitir();
                var sirali = char.IsDigit(liste.Groups[2].Value[0]);
                var etiket = sirali ? "ol" : "ul";
                var baslangic = sirali && int.TryParse(liste.Groups[2].Value.TrimEnd('.', ')'), out var b) && b != 1 ? $" start=\"{b}\"" : "";
                sb.Append('<').Append(etiket).Append(baslangic).Append('>');
                while (i < satirlar.Length)
                {
                    var m = ListeRx().Match(satirlar[i]);
                    if (!m.Success || char.IsDigit(m.Groups[2].Value[0]) != sirali) break;
                    var oge = m.Groups[3].Value;
                    i++;
                    // Girintili devam satırları aynı öğeye eklenir
                    while (i < satirlar.Length && satirlar[i].StartsWith("  ") && !ListeRx().IsMatch(satirlar[i]) && satirlar[i].Trim().Length > 0)
                        oge += "\n" + satirlar[i++].Trim();
                    var gorev = GorevRx().Match(oge);
                    sb.Append("<li>");
                    if (gorev.Success) sb.Append(gorev.Groups[1].Value is "x" or "X" ? "☑ " : "☐ ").Append(Satir(gorev.Groups[2].Value));
                    else sb.Append(string.Join("<br>", oge.Split('\n').Select(Satir)));
                    sb.Append("</li>");
                }
                sb.Append("</").Append(etiket).Append('>');
                continue;
            }

            paragraf.Add(s.TrimEnd());
            i++;
        }
        ParagrafBitir();
        return sb.ToString();
    }

    private static List<string> Hucreler(string satir)
    {
        var t = satir.Trim();
        if (t.StartsWith('|')) t = t[1..];
        if (t.EndsWith('|')) t = t[..^1];
        return t.Split('|').Select(h => h.Trim()).ToList();
    }

    /// <summary>Satır içi biçimlendirme. Önce kaçışlama, sonra kod parçaları korunarak diğer kurallar.</summary>
    internal static string Satir(string metin)
    {
        var kodlar = new List<string>();
        var s = SatirKodRx().Replace(metin, m => { kodlar.Add(m.Groups[1].Value); return $"\u0001{kodlar.Count - 1}\u0001"; });
        s = WebUtility.HtmlEncode(s);
        s = BaglantiRx().Replace(s, m =>
        {
            var url = WebUtility.HtmlDecode(m.Groups[2].Value);
            return GuvenliUrl(url) ? $"<a href=\"{WebUtility.HtmlEncode(url)}\" target=\"_blank\" rel=\"noopener noreferrer\">{m.Groups[1].Value}</a>" : m.Value;
        });
        s = OtomatikBaglantiRx().Replace(s, m => $"<a href=\"{m.Value}\" target=\"_blank\" rel=\"noopener noreferrer\">{m.Value}</a>");
        s = KalinRx().Replace(s, "<strong>$2</strong>");
        s = ItalikRx().Replace(s, "<em>$2</em>");
        s = CiziliRx().Replace(s, "<del>$1</del>");
        s = KodYerRx().Replace(s, m => "<code>" + WebUtility.HtmlEncode(kodlar[int.Parse(m.Groups[1].Value)]) + "</code>");
        return s;
    }

    internal static bool GuvenliUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) && u.Scheme is "http" or "https" or "mailto";

    [GeneratedRegex(@"^(#{1,6})\s+(.+)$")] private static partial Regex BaslikRx();
    [GeneratedRegex(@"^\s{0,3}([-*_])(\s*\1){2,}\s*$")] private static partial Regex CizgiRx();
    [GeneratedRegex(@"^\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)*\|?$")] private static partial Regex TabloAyracRx();
    [GeneratedRegex(@"^(\s{0,6})([-*+]|\d{1,3}[.)])\s+(.*)$")] private static partial Regex ListeRx();
    [GeneratedRegex(@"^\[([ xX])\]\s+(.*)$")] private static partial Regex GorevRx();
    [GeneratedRegex(@"`([^`\n]+)`")] private static partial Regex SatirKodRx();
    [GeneratedRegex("\u0001(\\d+)\u0001")] private static partial Regex KodYerRx();
    [GeneratedRegex(@"\[([^\]\n]+)\]\(([^)\s]+)\)")] private static partial Regex BaglantiRx();
    [GeneratedRegex(@"(?<![""=>])\bhttps?://(?:[^\s<&]|&amp;)*[^\s<&.,;:!?)\]]")] private static partial Regex OtomatikBaglantiRx();
    [GeneratedRegex(@"(\*\*|__)(?=\S)(.+?)(?<=\S)\1")] private static partial Regex KalinRx();
    [GeneratedRegex(@"(?<![\*\w])(\*|_)(?=\S)(.+?)(?<=\S)\1(?![\*\w])")] private static partial Regex ItalikRx();
    [GeneratedRegex(@"~~(?=\S)(.+?)(?<=\S)~~")] private static partial Regex CiziliRx();
    [GeneratedRegex(@"[^A-Za-z0-9#+\-_.]")] private static partial Regex DilTemizle();
}
