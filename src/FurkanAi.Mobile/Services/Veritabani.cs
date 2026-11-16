using FurkanAi.Core.Disa;
using FurkanAi.Core.Saglayicilar;
using SQLite;

namespace FurkanAi.Mobile.Services;

[Table("Sohbetler")]
public sealed class Sohbet
{
    [PrimaryKey, AutoIncrement] public long Id { get; set; }
    public string Baslik { get; set; } = "Yeni sohbet";
    public string Saglayici { get; set; } = "";
    public string Model { get; set; } = "";
    public string? AracKodu { get; set; }
    public string? SistemIstemi { get; set; }
    [Indexed] public bool Sabit { get; set; }
    public DateTime Olusturma { get; set; } = DateTime.UtcNow;
    [Indexed] public DateTime Guncelleme { get; set; } = DateTime.UtcNow;
    public int MesajSayisi { get; set; }
    public string? Onizleme { get; set; }
}

[Table("Mesajlar")]
public sealed class Mesaj
{
    [PrimaryKey, AutoIncrement] public long Id { get; set; }
    [Indexed] public long SohbetId { get; set; }
    public string Rol { get; set; } = nameof(Core.Saglayicilar.Rol.Kullanici);
    public string Metin { get; set; } = "";
    public string? EklerJson { get; set; }
    public DateTime Tarih { get; set; } = DateTime.UtcNow;
    public string? Saglayici { get; set; }
    public string? Model { get; set; }
    public bool Hata { get; set; }
    public bool Yarim { get; set; }          // kullanıcı durdurdu
    [Ignore] public List<EkKaydi> Ekler { get => Json.Oku<List<EkKaydi>>(EklerJson) ?? new(); set => EklerJson = value.Count == 0 ? null : Json.Yaz(value); }
    [Ignore] public Core.Saglayicilar.Rol RolDegeri => Enum.TryParse<Core.Saglayicilar.Rol>(Rol, out var r) ? r : Core.Saglayicilar.Rol.Kullanici;
}

public sealed record EkKaydi(string Ad, string Mime, long Boyut, string Dosya, string? MetinDosyasi);

[Table("Hafiza")]
public sealed class HafizaOgesi
{
    [PrimaryKey, AutoIncrement] public long Id { get; set; }
    public string Metin { get; set; } = "";
    public bool Aktif { get; set; } = true;
    public string Kaynak { get; set; } = "Elle";   // Elle | Mesajdan | Öneri
    public DateTime Olusturma { get; set; } = DateTime.UtcNow;
}

[Table("Gorseller")]
public sealed class GorselKaydi
{
    [PrimaryKey, AutoIncrement] public long Id { get; set; }
    public string Istem { get; set; } = "";
    public string Saglayici { get; set; } = "";
    public string Model { get; set; } = "";
    public string Dosya { get; set; } = "";
    public string Mime { get; set; } = "image/png";
    public DateTime Tarih { get; set; } = DateTime.UtcNow;
}

/// <summary>SQLite bağlantısı ve şema sürümü (PRAGMA user_version).</summary>
public sealed class Veritabani
{
    private SQLiteAsyncConnection? _b;
    private readonly SemaphoreSlim _kilit = new(1, 1);
    public static string Yol => Path.Combine(FileSystem.AppDataDirectory, "furkanai.db3");
    public static string EkKlasoru { get { var k = Path.Combine(FileSystem.AppDataDirectory, "ekler"); Directory.CreateDirectory(k); return k; } }
    public static string GorselKlasoru { get { var k = Path.Combine(FileSystem.AppDataDirectory, "gorseller"); Directory.CreateDirectory(k); return k; } }
    public const int SemaSurumu = 1;

    public async Task<SQLiteAsyncConnection> Baglanti()
    {
        if (_b != null) return _b;
        await _kilit.WaitAsync();
        try
        {
            if (_b != null) return _b;
            var b = new SQLiteAsyncConnection(Yol, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
            await b.EnableWriteAheadLoggingAsync();
            await b.CreateTablesAsync(CreateFlags.None, typeof(Sohbet), typeof(Mesaj), typeof(HafizaOgesi), typeof(GorselKaydi));
            var surum = await b.ExecuteScalarAsync<int>("PRAGMA user_version");
            if (surum < SemaSurumu) await b.ExecuteAsync($"PRAGMA user_version = {SemaSurumu}");
            _b = b;
            return b;
        }
        finally { _kilit.Release(); }
    }

    public long Boyut()
    {
        long t = 0;
        try { t += new FileInfo(Yol).Length; } catch { }
        foreach (var k in new[] { EkKlasoru, GorselKlasoru })
            try { t += Directory.GetFiles(k).Sum(f => new FileInfo(f).Length); } catch { }
        return t;
    }
}

/// <summary>Sohbet, mesaj, hafıza ve görsel kayıtları (repository).</summary>
public sealed class SohbetDeposu
{
    private readonly Veritabani _vt;
    public event Action? Degisti;
    public SohbetDeposu(Veritabani vt) => _vt = vt;
    private Task<SQLiteAsyncConnection> D => _vt.Baglanti();
    private void Bildir() => Degisti?.Invoke();

    // ---------- Sohbetler ----------
    public async Task<List<Sohbet>> Sohbetler(string? arama = null, int al = 200)
    {
        var d = await D;
        if (string.IsNullOrWhiteSpace(arama))
            return await d.QueryAsync<Sohbet>("SELECT * FROM Sohbetler ORDER BY Sabit DESC, Guncelleme DESC LIMIT ?", al);
        var a = "%" + arama.Trim() + "%";
        return await d.QueryAsync<Sohbet>(
            "SELECT * FROM Sohbetler WHERE Baslik LIKE ? OR Id IN (SELECT SohbetId FROM Mesajlar WHERE Metin LIKE ?) ORDER BY Sabit DESC, Guncelleme DESC LIMIT ?", a, a, al);
    }

    public async Task<Sohbet?> SohbetGetir(long id) => await (await D).FindAsync<Sohbet>(id);

    public async Task<long> SohbetKaydet(Sohbet s)
    {
        var d = await D;
        if (s.Id == 0) await d.InsertAsync(s); else await d.UpdateAsync(s);
        Bildir();
        return s.Id;
    }

    public async Task SohbetSil(long id)
    {
        var d = await D;
        var ekler = (await Mesajlar(id)).SelectMany(m => m.Ekler).ToList();
        await d.RunInTransactionAsync(c => { c.Execute("DELETE FROM Mesajlar WHERE SohbetId = ?", id); c.Delete<Sohbet>(id); });
        EkDosyalariniSil(ekler);
        Bildir();
    }

    public async Task HepsiniSil()
    {
        var d = await D;
        await d.DeleteAllAsync<Mesaj>(); await d.DeleteAllAsync<Sohbet>();
        try { foreach (var f in Directory.GetFiles(Veritabani.EkKlasoru)) File.Delete(f); } catch { }
        Bildir();
    }

    // ---------- Mesajlar ----------
    public async Task<List<Mesaj>> Mesajlar(long sohbetId) =>
        await (await D).Table<Mesaj>().Where(m => m.SohbetId == sohbetId).OrderBy(m => m.Id).ToListAsync();

    public async Task<long> MesajEkle(Mesaj m)
    {
        var d = await D;
        await d.InsertAsync(m);
        await OzetGuncelle(m.SohbetId);
        return m.Id;
    }

    public async Task MesajGuncelle(Mesaj m) { await (await D).UpdateAsync(m); await OzetGuncelle(m.SohbetId); }

    public async Task MesajSil(Mesaj m)
    {
        await (await D).DeleteAsync<Mesaj>(m.Id);
        EkDosyalariniSil(m.Ekler);
        await OzetGuncelle(m.SohbetId);
    }

    private async Task OzetGuncelle(long sohbetId)
    {
        var d = await D;
        var son = await d.Table<Mesaj>().Where(m => m.SohbetId == sohbetId).OrderByDescending(m => m.Id).FirstOrDefaultAsync();
        var sayi = await d.Table<Mesaj>().Where(m => m.SohbetId == sohbetId).CountAsync();
        await d.ExecuteAsync("UPDATE Sohbetler SET MesajSayisi = ?, Onizleme = ?, Guncelleme = ? WHERE Id = ?",
            sayi, son == null ? null : Core.Ortak.S.Kisalt(son.Metin, 120), DateTime.UtcNow, sohbetId);
        Bildir();
    }

    private static void EkDosyalariniSil(IEnumerable<EkKaydi> ekler)
    {
        foreach (var e in ekler)
            foreach (var f in new[] { e.Dosya, e.MetinDosyasi })
                if (f != null) try { File.Delete(Path.Combine(Veritabani.EkKlasoru, Path.GetFileName(f))); } catch { }
    }

    // ---------- Ek dosyaları ----------
    public static async Task<EkKaydi> EkSakla(EkIcerik e)
    {
        var ad = Guid.NewGuid().ToString("N");
        var uz = Path.GetExtension(e.Ad);
        await File.WriteAllBytesAsync(Path.Combine(Veritabani.EkKlasoru, ad + uz), e.Veri);
        string? metinDosyasi = null;
        if (!string.IsNullOrEmpty(e.Metin)) { metinDosyasi = ad + ".txt"; await File.WriteAllTextAsync(Path.Combine(Veritabani.EkKlasoru, metinDosyasi), e.Metin); }
        return new EkKaydi(e.Ad, e.Mime, e.Veri.LongLength, ad + uz, metinDosyasi);
    }

    public static async Task<EkIcerik?> EkYukle(EkKaydi k)
    {
        var yol = Path.Combine(Veritabani.EkKlasoru, Path.GetFileName(k.Dosya));
        if (!File.Exists(yol)) return null;
        string? metin = null;
        if (k.MetinDosyasi != null && File.Exists(Path.Combine(Veritabani.EkKlasoru, k.MetinDosyasi)))
            metin = await File.ReadAllTextAsync(Path.Combine(Veritabani.EkKlasoru, k.MetinDosyasi));
        return new EkIcerik(k.Ad, k.Mime, await File.ReadAllBytesAsync(yol), metin);
    }

    // ---------- Hafıza ----------
    public async Task<List<HafizaOgesi>> HafizaListesi() => await (await D).Table<HafizaOgesi>().OrderByDescending(h => h.Olusturma).ToListAsync();
    public async Task HafizaKaydet(HafizaOgesi h) { var d = await D; if (h.Id == 0) await d.InsertAsync(h); else await d.UpdateAsync(h); Bildir(); }
    public async Task HafizaSil(long id) { await (await D).DeleteAsync<HafizaOgesi>(id); Bildir(); }
    public async Task HafizayiTemizle() { await (await D).DeleteAllAsync<HafizaOgesi>(); Bildir(); }

    // ---------- Görseller ----------
    public async Task<List<GorselKaydi>> Gorseller() => await (await D).Table<GorselKaydi>().OrderByDescending(g => g.Tarih).ToListAsync();
    public async Task GorselEkle(GorselKaydi g) { await (await D).InsertAsync(g); Bildir(); }
    public async Task GorselSil(GorselKaydi g)
    {
        await (await D).DeleteAsync<GorselKaydi>(g.Id);
        try { File.Delete(Path.Combine(Veritabani.GorselKlasoru, Path.GetFileName(g.Dosya))); } catch { }
        Bildir();
    }

    // ---------- Dışa aktarma ----------
    public async Task<DisaSohbet> DisaHazirla(Sohbet s) =>
        new(s.Baslik, s.Olusturma, s.Saglayici, s.Model,
            (await Mesajlar(s.Id)).Where(m => !m.Hata).Select(m => new DisaMesaj(m.Rol, m.Metin, m.Tarih, m.Model, m.Ekler.Select(e => e.Ad).ToList())).ToList());

    public async Task<int> IceAktar(IEnumerable<DisaSohbet> sohbetler)
    {
        var n = 0;
        foreach (var s in sohbetler)
        {
            var yeni = new Sohbet { Baslik = s.Baslik, Saglayici = s.Saglayici ?? "", Model = s.Model ?? "", Olusturma = s.Olusturma, Guncelleme = s.Olusturma };
            await SohbetKaydet(yeni);
            foreach (var m in s.Mesajlar)
                await (await D).InsertAsync(new Mesaj { SohbetId = yeni.Id, Rol = m.Rol, Metin = m.Metin, Tarih = m.Tarih, Model = m.Model });
            await OzetGuncelle(yeni.Id);
            n++;
        }
        return n;
    }
}
