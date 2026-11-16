using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FurkanAi.Core.Saglayicilar;

namespace FurkanAi.Core.Dosyalar;

/// <summary>Ek dosyalarının türünü belirler ve metin çıkarır (TXT, DOCX; PDF için en iyi çaba).</summary>
public static partial class DosyaOkuyucu
{
    public const long AzamiBoyut = 20 * 1024 * 1024;      // 20 MB (sağlayıcıların satır içi sınırlarına yakın)
    public const long AzamiGorsel = 5 * 1024 * 1024;      // Claude görsel sınırı 5 MB
    public const int AzamiBaglamKarakter = 120_000;        // tek ek için modele gönderilen metin üst sınırı

    private static readonly HashSet<string> MetinUzantilari = new(StringComparer.OrdinalIgnoreCase)
    { ".txt", ".md", ".csv", ".json", ".xml", ".html", ".htm", ".log", ".cs", ".js", ".ts", ".py", ".java", ".kt", ".sql", ".yaml", ".yml", ".ini", ".css", ".razor" };

    public static EkTuru TurBul(string? mime, string? ad)
    {
        var m = (mime ?? "").ToLowerInvariant();
        var uz = Path.GetExtension(ad ?? "");
        if (m.StartsWith("image/") && m is not "image/svg+xml") return EkTuru.Gorsel;
        if (m == "application/pdf" || uz.Equals(".pdf", StringComparison.OrdinalIgnoreCase)) return EkTuru.Pdf;
        if (m == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" || uz.Equals(".docx", StringComparison.OrdinalIgnoreCase)) return EkTuru.Docx;
        if (m.StartsWith("text/") || m is "application/json" or "application/xml" || MetinUzantilari.Contains(uz)) return EkTuru.Metin;
        return EkTuru.Desteklenmeyen;
    }

    public static string MimeBul(string ad) => Path.GetExtension(ad).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", ".gif" => "image/gif", ".heic" => "image/heic",
        ".pdf" => "application/pdf", ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".json" => "application/json", ".md" => "text/markdown", ".csv" => "text/csv", _ => "text/plain"
    };

    /// <summary>Eki doğrular ve gerekiyorsa metnini çıkarır. Uygun değilse anlaşılır bir hata fırlatır.</summary>
    public static EkIcerik Hazirla(string ad, string? mime, byte[] veri)
    {
        mime = string.IsNullOrWhiteSpace(mime) || mime == "application/octet-stream" ? MimeBul(ad) : mime;
        if (veri.LongLength == 0) throw new InvalidDataException("Dosya boş.");
        if (veri.LongLength > AzamiBoyut) throw new InvalidDataException($"Dosya çok büyük ({Ortak.S.Boyut(veri.LongLength)}). En fazla {Ortak.S.Boyut(AzamiBoyut)}.");
        var tur = TurBul(mime, ad);
        return tur switch
        {
            EkTuru.Gorsel when veri.LongLength > AzamiGorsel => throw new InvalidDataException($"Görsel çok büyük ({Ortak.S.Boyut(veri.LongLength)}). En fazla {Ortak.S.Boyut(AzamiGorsel)}."),
            EkTuru.Gorsel => new EkIcerik(ad, mime, veri),
            EkTuru.Metin => new EkIcerik(ad, mime, veri, MetinCoz(veri)),
            EkTuru.Docx => new EkIcerik(ad, mime, veri, DocxMetni(veri)),
            EkTuru.Pdf => new EkIcerik(ad, "application/pdf", veri, PdfMetni(veri)),
            _ => throw new InvalidDataException("Bu dosya türü desteklenmiyor. Desteklenenler: görseller, PDF, DOCX, TXT ve diğer metin dosyaları.")
        };
    }

    /// <summary>UTF-8 (BOM'lu/BOM'suz), UTF-16 veya Windows-1254 metni çözer.</summary>
    public static string MetinCoz(byte[] veri)
    {
        if (veri.Length >= 2 && (veri[0] == 0xFF && veri[1] == 0xFE || veri[0] == 0xFE && veri[1] == 0xFF))
            return new StreamReader(new MemoryStream(veri), detectEncodingFromByteOrderMarks: true).ReadToEnd();
        try { return new UTF8Encoding(false, true).GetString(veri).TrimStart('﻿'); }
        catch (DecoderFallbackException)
        {
            try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); return Encoding.GetEncoding(1254).GetString(veri); }
            catch { return Encoding.Latin1.GetString(veri); }
        }
    }

    /// <summary>DOCX (Office Open XML) gövdesindeki paragrafları düz metne çevirir; tablolar sekmeyle ayrılır.</summary>
    public static string DocxMetni(byte[] veri)
    {
        try
        {
            using var zip = new ZipArchive(new MemoryStream(veri), ZipArchiveMode.Read);
            var giris = zip.GetEntry("word/document.xml") ?? throw new InvalidDataException("Geçerli bir DOCX değil (document.xml yok).");
            using var s = giris.Open();
            var x = XDocument.Load(s);
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            var sb = new StringBuilder();
            foreach (var p in x.Descendants(w + "p"))
            {
                if (p.Parent?.Name == w + "tc" && p.ElementsBeforeSelf(w + "p").Any()) sb.Append(' ');
                foreach (var e in p.Descendants())
                {
                    if (e.Name == w + "t") sb.Append(e.Value);
                    else if (e.Name == w + "tab") sb.Append('\t');
                    else if (e.Name == w + "br" || e.Name == w + "cr") sb.Append('\n');
                }
                sb.Append(p.Parent?.Name == w + "tc" ? "\t" : "\n");
            }
            return BosluklariSadelestir(sb.ToString());
        }
        catch (InvalidDataException) { throw; }
        catch (Exception e) { throw new InvalidDataException("DOCX okunamadı: dosya bozuk veya parola korumalı olabilir.", e); }
    }

    /// <summary>
    /// PDF'ten en iyi çabayla metin çıkarır (FlateDecode akışları, Tj/TJ işleçleri). Taranmış (görüntü) PDF'lerde
    /// veya özel yazı tipi kodlamalarında boş/eksik dönebilir — bu durumda PDF, destekleyen sağlayıcılara doğrudan gönderilir.
    /// </summary>
    public static string? PdfMetni(byte[] veri)
    {
        try
        {
            var latin = Encoding.Latin1.GetString(veri);
            if (!latin.StartsWith("%PDF")) throw new InvalidDataException("Geçerli bir PDF değil.");
            var sb = new StringBuilder();
            foreach (Match m in AkisRx().Matches(latin))
            {
                var sozluk = m.Groups[1].Value;
                var bas = m.Groups[2].Index;
                var uzunluk = m.Groups[2].Length;
                var ham = new byte[uzunluk];
                Array.Copy(veri, bas, ham, 0, uzunluk);
                string icerik;
                if (sozluk.Contains("/FlateDecode"))
                {
                    try
                    {
                        using var z = new ZLibStream(new MemoryStream(ham), CompressionMode.Decompress);
                        using var ms = new MemoryStream();
                        z.CopyTo(ms);
                        icerik = Encoding.Latin1.GetString(ms.ToArray());
                    }
                    catch { continue; }
                }
                else if (sozluk.Contains("/Filter")) continue;
                else icerik = Encoding.Latin1.GetString(ham);
                if (!icerik.Contains("BT")) continue;
                MetinIslecleri(icerik, sb);
                if (sb.Length > AzamiBaglamKarakter * 2) break;
            }
            var t = BosluklariSadelestir(sb.ToString());
            return t.Count(char.IsLetter) < 20 ? null : t;
        }
        catch (InvalidDataException) { throw; }
        catch { return null; }
    }

    private static void MetinIslecleri(string icerik, StringBuilder sb)
    {
        foreach (Match m in IslecRx().Matches(icerik))
        {
            if (m.Groups["tj"].Success) sb.Append(Dize(m.Groups["tj"].Value));
            else if (m.Groups["dizi"].Success)
            {
                foreach (Match p in DiziOgeRx().Matches(m.Groups["dizi"].Value))
                {
                    if (p.Groups[1].Success) sb.Append(Dize(p.Groups[1].Value));
                    else if (double.TryParse(p.Groups[2].Value, System.Globalization.NumberStyles.Float, Ortak.S.Inv, out var bosluk) && bosluk < -200) sb.Append(' ');
                }
            }
            else if (m.Groups["satir"].Success) sb.Append('\n');
            else if (m.Groups["td"].Success) { if (sb.Length > 0 && sb[^1] is not (' ' or '\n')) sb.Append(' '); }
            else if (m.Groups["et"].Success) sb.Append('\n');
        }
    }

    /// <summary>PDF metin dizesindeki kaçış dizilerini çözer: \n \( \) \\ ve sekizlik \ddd.</summary>
    internal static string Dize(string s)
    {
        var sb = new StringBuilder(s.Length);
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c != '\\' || i + 1 >= s.Length) { sb.Append(c); continue; }
            var n = s[++i];
            switch (n)
            {
                case 'n': sb.Append('\n'); break;
                case 'r': break;
                case 't': sb.Append('\t'); break;
                case 'b': case 'f': break;
                case '(': case ')': case '\\': sb.Append(n); break;
                default:
                    if (n is >= '0' and <= '7')
                    {
                        var j = i; var sekiz = "";
                        while (j < s.Length && sekiz.Length < 3 && s[j] is >= '0' and <= '7') sekiz += s[j++];
                        sb.Append((char)Convert.ToInt32(sekiz, 8));
                        i = j - 1;
                    }
                    else sb.Append(n);
                    break;
            }
        }
        return sb.ToString();
    }

    private static string BosluklariSadelestir(string s) =>
        CokluSatirRx().Replace(SatirSonuBoslukRx().Replace(s.Replace("\r", ""), "\n"), "\n\n").Trim();

    /// <summary>Çok uzun metinleri modele göndermeden önce kırpar ve kırpıldığını belirtir.</summary>
    public static string BaglamIcinKirp(string metin) =>
        metin.Length <= AzamiBaglamKarakter ? metin
        : metin[..AzamiBaglamKarakter] + $"\n\n[… metin çok uzun olduğu için ilk {AzamiBaglamKarakter:N0} karakter gönderildi]";

    [GeneratedRegex(@"<<((?:(?!>>).){0,800}?)>>\s*stream\r?\n(.*?)\r?\nendstream", RegexOptions.Singleline)] private static partial Regex AkisRx();
    [GeneratedRegex(@"\((?<tj>(?:\\.|[^\\)])*)\)\s*(?:Tj|'|"")|\[(?<dizi>(?:\\.|[^\]])*)\]\s*TJ|(?<satir>T\*)|(?<td>Td|TD)\b|(?<et>ET)\b", RegexOptions.Singleline)] private static partial Regex IslecRx();
    [GeneratedRegex(@"\(((?:\\.|[^\\)])*)\)|(-?\d+(?:\.\d+)?)")] private static partial Regex DiziOgeRx();
    [GeneratedRegex(@"[ \t]+\n")] private static partial Regex SatirSonuBoslukRx();
    [GeneratedRegex(@"\n{3,}")] private static partial Regex CokluSatirRx();
}
