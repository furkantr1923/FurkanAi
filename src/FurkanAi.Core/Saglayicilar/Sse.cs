using System.Runtime.CompilerServices;
using System.Text;

namespace FurkanAi.Core.Saglayicilar;

public readonly record struct SseOlayi(string? Olay, string Veri);

/// <summary>Server-Sent Events (text/event-stream) okuyucu. Çok satırlı "data:" alanlarını birleştirir.</summary>
public static class SseOkuyucu
{
    public static async IAsyncEnumerable<SseOlayi> Oku(Stream akis, [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var r = new StreamReader(akis, Encoding.UTF8);
        await foreach (var o in Oku(r, ct)) yield return o;
    }

    public static async IAsyncEnumerable<SseOlayi> Oku(TextReader r, [EnumeratorCancellation] CancellationToken ct = default)
    {
        string? olay = null;
        var veri = new StringBuilder();
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var satir = await r.ReadLineAsync(ct);
            if (satir == null)
            {
                if (veri.Length > 0) yield return new SseOlayi(olay, veri.ToString());
                yield break;
            }
            if (satir.Length == 0)
            {
                if (veri.Length > 0) yield return new SseOlayi(olay, veri.ToString());
                olay = null; veri.Clear();
                continue;
            }
            if (satir[0] == ':') continue; // yorum / canlı tutma
            var i = satir.IndexOf(':');
            var alan = i < 0 ? satir : satir[..i];
            var deger = i < 0 ? "" : satir[(i + 1)..];
            if (deger.StartsWith(' ')) deger = deger[1..];
            switch (alan)
            {
                case "event": olay = deger; break;
                case "data":
                    if (veri.Length > 0) veri.Append('\n');
                    veri.Append(deger);
                    break;
            }
        }
    }
}
