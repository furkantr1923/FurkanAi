using System.IO.Compression;
using System.Text;
using FurkanAi.Core.Araclar;
using FurkanAi.Core.Disa;
using FurkanAi.Core.Dosyalar;
using FurkanAi.Core.Hafiza;
using FurkanAi.Core.Metin;
using FurkanAi.Core.Saglayicilar;

namespace FurkanAi.Tests;

public class MetinVeDosyaTestleri
{
    [Fact]
    public void Markdown_ham_html_ve_tehlikeli_baglantilari_kacislar()
    {
        var h = MarkdownCizici.Html("<script>alert(1)</script> [tıkla](javascript:alert(1)) <img src=x onerror=alert(1)>");
        Assert.DoesNotContain("<script>", h);
        Assert.DoesNotContain("<img", h);
        Assert.DoesNotContain("href=\"javascript", h);
        Assert.Contains("&lt;script&gt;", h);
    }

    [Fact]
    public void Markdown_temel_ogeler()
    {
        var h = MarkdownCizici.Html("# Başlık\n\n**kalın** ve *italik* `kod` [site](https://ornek.com)\n\n- a\n- b\n\n1. bir\n2. iki\n\n> alıntı\n\n| A | B |\n|---|---|\n| 1 | 2 |\n\n```csharp\nvar x = 1 < 2;\n```");
        Assert.Contains("<h3>Başlık</h3>", h);
        Assert.Contains("<strong>kalın</strong>", h);
        Assert.Contains("<em>italik</em>", h);
        Assert.Contains("<code>kod</code>", h);
        Assert.Contains("href=\"https://ornek.com\"", h);
        Assert.Contains("<ul><li>a</li><li>b</li></ul>", h);
        Assert.Contains("<ol><li>bir</li><li>iki</li></ol>", h);
        Assert.Contains("<blockquote>", h);
        Assert.Contains("<th>A</th>", h);
        Assert.Contains("class=\"dil-csharp\"", h);
        Assert.Contains("var x = 1 &lt; 2;", h);
    }

    [Fact]
    public void Markdown_akista_kapanmamis_kod_blogunu_gosterir()
    {
        var h = MarkdownCizici.Html("Örnek:\n```python\nprint('merhaba')");
        Assert.Contains("print(&#39;merhaba&#39;)", h);
    }

    [Fact]
    public void Markdown_kod_icindeki_yildizlar_bicimlenmez()
    {
        Assert.Contains("<code>a*b*c</code>", MarkdownCizici.Html("`a*b*c`"));
        Assert.DoesNotContain("<em>", MarkdownCizici.Html("dosya_adi_uzun"));
    }

    [Fact]
    public void Docx_paragraflari_okunur()
    {
        using var ms = new MemoryStream();
        using (var z = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            using var w = new StreamWriter(z.CreateEntry("word/document.xml").Open());
            w.Write("<?xml version=\"1.0\"?><w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>" +
                    "<w:p><w:r><w:t>Merhaba </w:t></w:r><w:r><w:t>dünya</w:t></w:r></w:p><w:p><w:r><w:t>İkinci</w:t><w:tab/><w:t>satır</w:t></w:r></w:p></w:body></w:document>");
        }
        var t = DosyaOkuyucu.DocxMetni(ms.ToArray());
        Assert.Equal("Merhaba dünya\nİkinci\tsatır", t);
    }

    [Fact]
    public void Pdf_basit_metin_akisi_okunur()
    {
        var icerik = "BT /F1 12 Tf 72 712 Td (Merhaba PDF dunyasi, bu bir deneme metnidir) Tj T* [(Ikinci) -250 (satir)] TJ ET";
        var pdf = $"%PDF-1.4\n1 0 obj\n<< /Length {icerik.Length} >>\nstream\n{icerik}\nendstream\nendobj\n%%EOF";
        var t = DosyaOkuyucu.PdfMetni(Encoding.Latin1.GetBytes(pdf));
        Assert.NotNull(t);
        Assert.Contains("Merhaba PDF dunyasi", t);
        Assert.Contains("Ikinci satir", t);
        Assert.Equal("a(b)\\c", DosyaOkuyucu.Dize(@"a\(b\)\\c"));
    }

    [Fact]
    public void Ek_turu_ve_boyut_dogrulama()
    {
        Assert.Equal(EkTuru.Gorsel, DosyaOkuyucu.TurBul("image/jpeg", "a.jpg"));
        Assert.Equal(EkTuru.Pdf, DosyaOkuyucu.TurBul(null, "rapor.PDF"));
        Assert.Equal(EkTuru.Metin, DosyaOkuyucu.TurBul("application/octet-stream", "kod.py"));
        Assert.Equal(EkTuru.Desteklenmeyen, DosyaOkuyucu.TurBul("application/zip", "a.zip"));
        Assert.Throws<InvalidDataException>(() => DosyaOkuyucu.Hazirla("a.zip", "application/zip", new byte[10]));
        Assert.Throws<InvalidDataException>(() => DosyaOkuyucu.Hazirla("b.png", "image/png", new byte[DosyaOkuyucu.AzamiGorsel + 1]));
        Assert.Equal("çğış", DosyaOkuyucu.Hazirla("t.txt", "text/plain", Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("çğış")).ToArray()).Metin);
    }

    [Fact]
    public void Araclar_on_bir_tane_ve_secenekler_yerlesir()
    {
        Assert.Equal(11, AracKatalogu.Hepsi.Count);
        Assert.Equal(AracKatalogu.Hepsi.Count, AracKatalogu.Hepsi.Select(a => a.Kod).Distinct().Count());
        var cevir = AracKatalogu.Bul("cevir")!;
        Assert.Contains("Almanca", cevir.Istem(new Dictionary<string, string> { ["dil"] = "Almanca" }));
        Assert.DoesNotContain("{", AracKatalogu.Bul("eposta")!.Istem(new Dictionary<string, string>()));
    }

    [Fact]
    public void Hafiza_hassas_bilgiyi_reddeder_onerileri_cozer()
    {
        Assert.NotNull(HafizaYardimcisi.Dogrula("Benim API anahtarım sk-abcdefghijklmnopqrstuvwx"));
        Assert.NotNull(HafizaYardimcisi.Dogrula("wifi şifre: 1234"));
        Assert.Null(HafizaYardimcisi.Dogrula("Kısa yanıtları tercih ediyor"));
        var o = HafizaYardimcisi.OnerileriCoz("İşte:\n```json\n[\"C# geliştiricisi\", \"Kısa yanıt sever\"]\n```");
        Assert.Equal(new[] { "C# geliştiricisi", "Kısa yanıt sever" }, o);
        Assert.Empty(HafizaYardimcisi.OnerileriCoz("öneri yok"));
        Assert.Null(HafizaYardimcisi.SistemBlogu(Array.Empty<string>()));
    }

    [Fact]
    public void Disa_aktarim_json_gidis_donus_ve_metin_bicimleri()
    {
        var s = new DisaSohbet("Deneme", DateTime.UtcNow, "OpenAI", "gpt-5-mini",
            new[] { new DisaMesaj("Kullanici", "Selam", DateTime.UtcNow, null, new[] { "a.png" }), new DisaMesaj("Asistan", "Merhaba!", DateTime.UtcNow, "gpt-5-mini", Array.Empty<string>()) });
        var geri = SohbetDisaAktarici.JsonOku(SohbetDisaAktarici.Json(new[] { s }));
        Assert.Equal("Deneme", geri[0].Baslik);
        Assert.Equal(2, geri[0].Mesajlar.Count);
        Assert.Contains("FURKAN AI:", SohbetDisaAktarici.Txt(s));
        Assert.Contains("# Deneme", SohbetDisaAktarici.Markdown(s));
        Assert.Throws<InvalidDataException>(() => SohbetDisaAktarici.JsonOku("{\"bicim\":\"baska\"}"));
    }
}
