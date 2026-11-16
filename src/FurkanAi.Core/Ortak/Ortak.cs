using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FurkanAi.Core.Ortak;

/// <summary>Küçük yardımcılar: kültür, kısaltma, dosya adı.</summary>
public static class S
{
    public static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    public static readonly CultureInfo Tr = new("tr-TR");

    public static string Kisalt(string? s, int azami)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.ReplaceLineEndings(" ").Trim();
        return s.Length <= azami ? s : s[..Math.Max(0, azami - 1)].TrimEnd() + "…";
    }

    public static string DosyaAdi(string s, int azami = 60)
    {
        var gecersiz = Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }).ToHashSet();
        var sb = new StringBuilder();
        foreach (var c in s.Trim()) sb.Append(gecersiz.Contains(c) || char.IsControl(c) ? '_' : c);
        var r = sb.ToString().Trim(' ', '.');
        if (r.Length > azami) r = r[..azami];
        return r.Length == 0 ? "dosya" : r;
    }

    /// <summary>Byte boyutunu okunur biçimde verir.</summary>
    public static string Boyut(long b) => b < 1024 ? $"{b} B" : b < 1024 * 1024 ? (b / 1024.0).ToString("0.#", Tr) + " KB" : (b / 1048576.0).ToString("0.#", Tr) + " MB";
}

public static class JsonAyar
{
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    public static readonly JsonSerializerOptions Girintili = new(Web) { WriteIndented = true };
}
