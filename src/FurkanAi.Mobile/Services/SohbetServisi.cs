using System.Collections.Concurrent;
using System.Text;
using FurkanAi.Core.Araclar;
using FurkanAi.Core.Hafiza;
using FurkanAi.Core.Ortak;
using FurkanAi.Core.Saglayicilar;
using FurkanAi.Mobile.Platforms.Android;

namespace FurkanAi.Mobile.Services;

/// <summary>Devam eden bir yanıt akışı. Sayfadan çıkılsa da sürer; bitince veritabanına yazılır.</summary>
public sealed class Akis
{
    public long SohbetId { get; init; }
    public long MesajId { get; set; }
    public StringBuilder Metin { get; } = new();
    public CancellationTokenSource Iptal { get; } = new();
    public bool Bitti { get; set; }
    public DateTime Baslangic { get; } = DateTime.UtcNow;
}

/// <summary>Sohbet akışı: bağlam kurma, akışlı yanıt, durdurma, yeniden üretme, hata kaydı, bildirim.</summary>
public sealed class SohbetServisi
{
    private readonly SohbetDeposu _depo;
    private readonly SaglayiciServisi _sag;
    private readonly Ayarlar _ayar;
    private readonly SesServisi _ses;
    private readonly UygulamaYasamDongusu _yasam;
    private readonly ConcurrentDictionary<long, Akis> _akislar = new();

    /// <summary>Akış güncellemesi (sohbet id). Arayüz bu olayla yeniden çizilir.</summary>
    public event Action<long>? AkisDegisti;

    /// <summary>Geçmiş kaydı kapalıyken kullanılan geçici (bellekteki) sohbetler.</summary>
    private readonly ConcurrentDictionary<long, (Sohbet Sohbet, List<Mesaj> Mesajlar)> _gecici = new();
    private long _geciciNo = -1;

    public const string TemelIstem =
        "Sen FURKAN AI adlı yardımsever bir asistansın. Kullanıcıyla aynı dilde yanıt ver (aksi istenmedikçe Türkçe). " +
        "Açık, doğru ve düzenli ol; gerektiğinde Markdown (başlık, liste, tablo, kod bloğu) kullan. " +
        "Emin olmadığın bilgileri uydurma, belirsizliği belirt. Güncel olaylara erişimin olmayabilir; bunu gerektiğinde söyle.";

    public SohbetServisi(SohbetDeposu depo, SaglayiciServisi sag, Ayarlar ayar, SesServisi ses, UygulamaYasamDongusu yasam)
    { _depo = depo; _sag = sag; _ayar = ayar; _ses = ses; _yasam = yasam; }

    public Akis? AktifAkis(long sohbetId) => _akislar.TryGetValue(sohbetId, out var a) && !a.Bitti ? a : null;
    public bool HerhangiAkisVar => _akislar.Values.Any(a => !a.Bitti);

    // ---------- Sohbet/mesaj erişimi (kalıcı veya geçici) ----------
    public async Task<Sohbet?> SohbetGetir(long id) => id < 0 ? (_gecici.TryGetValue(id, out var g) ? g.Sohbet : null) : await _depo.SohbetGetir(id);
    public async Task<List<Mesaj>> Mesajlar(long id) => id < 0 ? (_gecici.TryGetValue(id, out var g) ? g.Mesajlar.ToList() : new()) : await _depo.Mesajlar(id);

    public async Task<Sohbet> YeniSohbet(string? aracKodu = null, string? sistemIstemi = null)
    {
        var t = _ayar.AktifSaglayici;
        var s = new Sohbet { Saglayici = t.ToString(), Model = _ayar.Model(t), AracKodu = aracKodu, SistemIstemi = sistemIstemi };
        if (aracKodu != null && AracKatalogu.Bul(aracKodu) is { } a) s.Baslik = a.Ad;
        if (_ayar.GecmisiKaydet) await _depo.SohbetKaydet(s);
        else { s.Id = Interlocked.Decrement(ref _geciciNo); _gecici[s.Id] = (s, new()); }
        return s;
    }

    private async Task MesajEkle(Mesaj m)
    {
        if (m.SohbetId < 0) { if (_gecici.TryGetValue(m.SohbetId, out var g)) { m.Id = g.Mesajlar.Count == 0 ? 1 : g.Mesajlar.Max(x => x.Id) + 1; g.Mesajlar.Add(m); } return; }
        await _depo.MesajEkle(m);
    }

    private async Task MesajGuncelle(Mesaj m) { if (m.SohbetId > 0) await _depo.MesajGuncelle(m); }

    public async Task MesajSil(Mesaj m)
    {
        if (m.SohbetId < 0) { if (_gecici.TryGetValue(m.SohbetId, out var g)) g.Mesajlar.RemoveAll(x => x.Id == m.Id); }
        else await _depo.MesajSil(m);
        AkisDegisti?.Invoke(m.SohbetId);
    }

    public async Task SohbetGuncelle(Sohbet s) { if (s.Id > 0) await _depo.SohbetKaydet(s); }

    // ---------- Gönderme ----------
    /// <summary>Kullanıcı mesajını kaydeder ve yanıt akışını başlatır (beklemeden döner).</summary>
    public async Task Gonder(Sohbet s, string metin, IReadOnlyList<EkIcerik> ekler)
    {
        if (AktifAkis(s.Id) != null) throw new InvalidOperationException("Önceki yanıt hâlâ yazılıyor. Durdurup tekrar deneyin.");
        var kayitlar = new List<EkKaydi>();
        foreach (var e in ekler) kayitlar.Add(await SohbetDeposu.EkSakla(e));
        var m = new Mesaj { SohbetId = s.Id, Rol = nameof(Rol.Kullanici), Metin = metin.Trim(), Ekler = kayitlar };
        await MesajEkle(m);
        if (s.MesajSayisi == 0 && (s.Baslik == "Yeni sohbet" || s.AracKodu != null))
        {
            var ozet = metin.Trim().Length > 0 ? S.Kisalt(metin, 48) : ekler.FirstOrDefault()?.Ad ?? "Yeni sohbet";
            s.Baslik = s.AracKodu != null && AracKatalogu.Bul(s.AracKodu) is { } a ? $"{a.Ad}: {ozet}" : ozet;
        }
        s.MesajSayisi++;
        s.Onizleme = S.Kisalt(m.Metin.Length > 0 ? m.Metin : "📎 " + string.Join(", ", ekler.Select(e => e.Ad)), 120);
        s.Guncelleme = DateTime.UtcNow;
        await SohbetGuncelle(s);
        _ = Task.Run(() => Calistir(s));
    }

    /// <summary>Son asistan yanıtını (ve varsa hatayı) silip yeniden üretir.</summary>
    public async Task YenidenUret(Sohbet s)
    {
        if (AktifAkis(s.Id) != null) return;
        var l = await Mesajlar(s.Id);
        for (var i = l.Count - 1; i >= 0 && l[i].RolDegeri == Rol.Asistan; i--) await MesajSil(l[i]);
        _ = Task.Run(() => Calistir(s));
    }

    public void Durdur(long sohbetId) { if (_akislar.TryGetValue(sohbetId, out var a)) a.Iptal.Cancel(); }

    /// <summary>Sistem istemi: temel kişilik + kullanıcı istemi + yanıt dili + onaylı hafıza + araç istemi.</summary>
    private async Task<string> SistemIstemi(Sohbet s)
    {
        var p = new List<string> { TemelIstem };
        if (!string.IsNullOrWhiteSpace(_ayar.KisiselIstem)) p.Add("Kullanıcının genel talimatları:\n" + _ayar.KisiselIstem.Trim());
        if (_ayar.YanitDili != "otomatik") p.Add($"Yanıtlarını her zaman {_ayar.YanitDili} dilinde ver.");
        if (_ayar.HafizaAktif)
        {
            var blok = HafizaYardimcisi.SistemBlogu((await _depo.HafizaListesi()).Where(h => h.Aktif).Select(h => h.Metin));
            if (blok != null) p.Add(blok);
        }
        if (!string.IsNullOrWhiteSpace(s.SistemIstemi)) p.Add(s.SistemIstemi);
        return string.Join("\n\n", p);
    }

    /// <summary>Son N mesajı sağlayıcı biçimine çevirir. Ekler yalnızca son 6 mesajda tam gönderilir (maliyet/sınır).</summary>
    private async Task<List<SohbetMesaji>> Baglam(long sohbetId)
    {
        var l = (await Mesajlar(sohbetId)).Where(m => !m.Hata && (m.Metin.Length > 0 || m.Ekler.Count > 0)).TakeLast(Math.Clamp(_ayar.BaglamMesajSayisi, 2, 100)).ToList();
        var sonuc = new List<SohbetMesaji>();
        for (var i = 0; i < l.Count; i++)
        {
            var m = l[i];
            var metin = m.Metin;
            var ekler = new List<EkIcerik>();
            if (m.Ekler.Count > 0)
            {
                if (i >= l.Count - 6)
                    foreach (var k in m.Ekler)
                    {
                        var e = await SohbetDeposu.EkYukle(k);
                        if (e != null) ekler.Add(e); else metin += $"\n[Ek bulunamadı: {k.Ad}]";
                    }
                else metin += "\n[Önceki ekler: " + string.Join(", ", m.Ekler.Select(e => e.Ad)) + "]";
            }
            sonuc.Add(new SohbetMesaji(m.RolDegeri, metin, ekler));
        }
        return sonuc;
    }

    private async Task Calistir(Sohbet s)
    {
        var t = Enum.TryParse<SaglayiciTuru>(s.Saglayici, out var st) ? st : _ayar.AktifSaglayici;
        var model = string.IsNullOrWhiteSpace(s.Model) ? _ayar.Model(t) : s.Model;
        var akis = new Akis { SohbetId = s.Id };
        _akislar[s.Id] = akis;
        var cevap = new Mesaj { SohbetId = s.Id, Rol = nameof(Rol.Asistan), Saglayici = ModelKaydi.Tanim(t).Ad, Model = model };
        AkisDegisti?.Invoke(s.Id);
        try
        {
            if (!_sag.BaglantiYeterli(t)) throw new SaglayiciHatasi("İnternet bağlantısı yok. Geçmiş sohbetlerinizi çevrimdışı okuyabilirsiniz; yeni yanıt için bağlantı gerekir.");
            var istemci = await _sag.Istemci(t);
            var secenek = new IstekSecenekleri
            {
                Model = model, SistemIstemi = await SistemIstemi(s), AzamiToken = Math.Clamp(_ayar.AzamiToken, 256, 64000),
                Sicaklik = _ayar.Sicaklik < 0 ? null : _ayar.Sicaklik
            };
            var baglam = await Baglam(s.Id);
            var son = DateTime.MinValue;
            await foreach (var parca in istemci.Akis(baglam, secenek, akis.Iptal.Token))
            {
                akis.Metin.Append(parca);
                if ((DateTime.UtcNow - son).TotalMilliseconds > 50) { son = DateTime.UtcNow; AkisDegisti?.Invoke(s.Id); }
            }
            cevap.Metin = akis.Metin.ToString();
            if (cevap.Metin.Trim().Length == 0) throw new SaglayiciHatasi("Model boş yanıt döndürdü. Farklı bir model deneyin veya isteği yeniden yazın.");
        }
        catch (OperationCanceledException) when (akis.Iptal.IsCancellationRequested)
        {
            cevap.Metin = akis.Metin.ToString();
            cevap.Yarim = true;
            if (cevap.Metin.Length == 0) { cevap.Metin = "⏹ Yanıt durduruldu."; cevap.Hata = true; }
        }
        catch (Exception e)
        {
            if (e is not SaglayiciHatasi) HataKaydi.Yaz("Sohbet akışı", e);
            var kismi = akis.Metin.ToString();
            if (kismi.Length > 0) { cevap.Metin = kismi; cevap.Yarim = true; await MesajEkle(cevap); cevap = new Mesaj { SohbetId = s.Id, Rol = nameof(Rol.Asistan), Model = model }; }
            cevap.Metin = "⚠️ " + HataMesaji.Olustur(e is SaglayiciHatasi ? e : new SaglayiciHatasi(HataMesaji.Olustur(e), null, true, e));
            cevap.Hata = true;
        }
        finally
        {
            akis.Bitti = true;
        }
        try
        {
            await MesajEkle(cevap);
            s.MesajSayisi++;
            if (s.Id < 0 && _gecici.TryGetValue(s.Id, out var g)) g.Sohbet.Guncelleme = DateTime.UtcNow;
        }
        catch (Exception e) { HataKaydi.Yaz("Yanıt kaydı", e); }
        _akislar.TryRemove(s.Id, out _);
        AkisDegisti?.Invoke(s.Id);

        if (!cevap.Hata)
        {
            if (_yasam.ArkaPlanda && _ayar.YanitBildirimi && s.Id > 0)
                AndroidBildirim.Goster((int)(s.Id % 100000) + 100, "FURKAN AI yanıtladı ✨", S.Kisalt(SesServisi.Temizle(cevap.Metin), 140), s.Id);
            else if (!_yasam.ArkaPlanda && _ayar.OtomatikSesliOku)
                _ = _ses.Oku(cevap.Metin, cevap.Id);
        }
    }

    // ---------- Hafıza önerisi ----------
    /// <summary>Modelden bu sohbet için hafıza ÖNERİLERİ ister. Hiçbiri kullanıcı onayı olmadan kaydedilmez.</summary>
    public async Task<List<string>> HafizaOnerileri(Sohbet s, CancellationToken ct)
    {
        var t = Enum.TryParse<SaglayiciTuru>(s.Saglayici, out var st) ? st : _ayar.AktifSaglayici;
        var mesajlar = (await Mesajlar(s.Id)).Where(m => !m.Hata).TakeLast(30)
            .Select(m => $"{(m.RolDegeri == Rol.Kullanici ? "Kullanıcı" : "Asistan")}: {S.Kisalt(m.Metin, 1500)}");
        var istemci = await _sag.Istemci(t);
        var sb = new StringBuilder();
        await foreach (var p in istemci.Akis(new[] { new SohbetMesaji(Rol.Kullanici, string.Join("\n", mesajlar)) },
                           new IstekSecenekleri { Model = s.Model, SistemIstemi = HafizaYardimcisi.OneriIstemi, AzamiToken = 800 }, ct))
            sb.Append(p);
        var mevcut = (await _depo.HafizaListesi()).Select(h => h.Metin).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return HafizaYardimcisi.OnerileriCoz(sb.ToString()).Where(o => !mevcut.Contains(o)).ToList();
    }
}
