using System.Text;
using System.Text.Json;

namespace FurkanAi.Core.Disa;

public sealed record DisaMesaj(string Rol, string Metin, DateTime Tarih, string? Model, IReadOnlyList<string> Ekler);
public sealed record DisaSohbet(string Baslik, DateTime Olusturma, string? Saglayici, string? Model, IReadOnlyList<DisaMesaj> Mesajlar);

/// <summary>Sohbetleri JSON / TXT / Markdown olarak dışa aktarır. API anahtarı veya ek dosya içeriği yazılmaz.</summary>
public static class SohbetDisaAktarici
{
    public const string Bicim = "FURKAN-AI-SOHBET";

    public static string Json(IReadOnlyList<DisaSohbet> sohbetler) =>
        JsonSerializer.Serialize(new { bicim = Bicim, surum = 1, olusturma = DateTime.UtcNow, sohbetler }, Ortak.JsonAyar.Girintili);

    public static List<DisaSohbet> JsonOku(string json)
    {
        using var d = JsonDocument.Parse(json);
        if (!d.RootElement.TryGetProperty("bicim", out var b) || b.GetString() != Bicim) throw new InvalidDataException("Bu dosya bir FURKAN AI sohbet dışa aktarımı değil.");
        return d.RootElement.GetProperty("sohbetler").Deserialize<List<DisaSohbet>>(Ortak.JsonAyar.Web) ?? new();
    }

    private static string RolAdi(string r) => r switch { "Kullanici" => "Siz", "Asistan" => "FURKAN AI", _ => r };

    public static string Txt(DisaSohbet s)
    {
        var sb = new StringBuilder();
        sb.AppendLine(s.Baslik).AppendLine(new string('=', Math.Min(60, Math.Max(3, s.Baslik.Length))));
        sb.AppendLine($"{s.Olusturma.ToLocalTime():dd.MM.yyyy HH:mm} · {s.Saglayici} {s.Model}").AppendLine();
        foreach (var m in s.Mesajlar)
        {
            sb.AppendLine($"[{m.Tarih.ToLocalTime():HH:mm}] {RolAdi(m.Rol)}:");
            if (m.Ekler.Count > 0) sb.AppendLine("  (Ekler: " + string.Join(", ", m.Ekler) + ")");
            sb.AppendLine(m.Metin).AppendLine();
        }
        return sb.ToString();
    }

    public static string Markdown(DisaSohbet s)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {s.Baslik}").AppendLine();
        sb.AppendLine($"*{s.Olusturma.ToLocalTime():dd.MM.yyyy HH:mm} · {s.Saglayici} · {s.Model}*").AppendLine();
        foreach (var m in s.Mesajlar)
        {
            sb.AppendLine($"### {(m.Rol == "Kullanici" ? "🧑" : "🤖")} {RolAdi(m.Rol)}");
            if (m.Ekler.Count > 0) sb.AppendLine("> 📎 " + string.Join(", ", m.Ekler)).AppendLine();
            sb.AppendLine(m.Metin).AppendLine();
        }
        return sb.ToString();
    }
}
