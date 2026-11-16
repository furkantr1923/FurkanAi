namespace FurkanAi.Core.Araclar;

public sealed record AracSecenegi(string Kod, string Etiket, IReadOnlyList<string> Degerler);

/// <summary>Hazır yapay zekâ aracı: bir sistem istemi + kullanıcı girdisi şablonu.</summary>
public sealed record AracTanimi(string Kod, string Ad, string Ikon, string Aciklama, string GirdiEtiketi, string Ornek,
    string SistemIstemi, IReadOnlyList<AracSecenegi> Secenekler, bool DosyaKabulEder = false)
{
    /// <summary>Seçenek değerlerini ({dil}, {ton}…) sistem istemine yerleştirir.</summary>
    public string Istem(IReadOnlyDictionary<string, string> secimler)
    {
        var s = SistemIstemi;
        foreach (var o in Secenekler) s = s.Replace("{" + o.Kod + "}", secimler.TryGetValue(o.Kod, out var d) ? d : o.Degerler[0]);
        return s;
    }
}

public static class AracKatalogu
{
    private static readonly string[] Diller = { "İngilizce", "Türkçe", "Almanca", "Fransızca", "İspanyolca", "Arapça", "Rusça", "Japonca", "Çince", "İtalyanca" };
    private static readonly string[] Tonlar = { "Profesyonel", "Samimi", "Resmî", "Kısa ve net", "İkna edici" };
    private const string Ortak = " Yanıtını Markdown ile, açık ve düzenli biçimde ver. Emin olmadığın bilgileri uydurma; belirsizlik varsa belirt.";

    public static readonly IReadOnlyList<AracTanimi> Hepsi = new AracTanimi[]
    {
        new("ozet", "Özetle", "ozet", "Uzun metin veya belgeyi özetler", "Özetlenecek metin (veya dosya ekleyin)", "Toplantı notlarını, makaleyi veya raporu buraya yapıştırın…",
            "Sen bir özetleme uzmanısın. Verilen içeriği {uzunluk} biçimde özetle: önce tek cümlelik ana fikir, sonra madde madde önemli noktalar, varsa sayılar/tarihler ve yapılacaklar." + Ortak,
            new[] { new AracSecenegi("uzunluk", "Uzunluk", new[] { "kısa", "orta uzunlukta", "ayrıntılı" }) }, DosyaKabulEder: true),
        new("cevir", "Çevir", "dil", "Metni istenen dile doğal biçimde çevirir", "Çevrilecek metin", "Merhaba, toplantıyı yarın 10:00'a alabilir miyiz?",
            "Sen profesyonel bir çevirmensin. Metni {dil} diline, anlamı ve tonu koruyarak doğal biçimde çevir. Yalnızca çeviriyi ver; gerekirse sonunda kısa bir not ekle.",
            new[] { new AracSecenegi("dil", "Hedef dil", Diller) }),
        new("duzelt", "Yazım düzelt", "kalem", "Yazım, dil bilgisi ve noktalama düzeltir", "Düzeltilecek metin", "bugun toplantiya gelemiyecem cunku hastayim",
            "Sen bir editörsün. Metnin yazım, dil bilgisi ve noktalama hatalarını düzelt; anlamı değiştirme. Önce düzeltilmiş metni ver, ardından 'Değişiklikler' başlığıyla önemli düzeltmeleri kısaca listele.",
            Array.Empty<AracSecenegi>()),
        new("eposta", "E-posta yaz", "eposta", "Kısa notlardan e-posta taslağı hazırlar", "E-postanın amacı / notlar", "Müşteriye teslimatın 3 gün gecikeceğini, özür dileyerek bildir",
            "Sen deneyimli bir iletişim uzmanısın. Verilen notlardan {ton} tonda, Türkçe bir e-posta taslağı yaz: Konu satırı, selamlama, gövde ve kapanış. Köşeli parantezle [İsim] gibi doldurulacak yerleri belirt." + Ortak,
            new[] { new AracSecenegi("ton", "Ton", Tonlar) }),
        new("kod-acikla", "Kod açıkla", "kod", "Kodu satır satır anlaşılır şekilde açıklar", "Kod", "for i in range(10):\n    print(i ** 2)",
            "Sen sabırlı bir yazılım eğitmenisin. Verilen kodun ne yaptığını önce bir paragrafta, sonra önemli bölümleri adım adım açıkla. Olası hataları, güvenlik sorunlarını ve iyileştirme önerilerini ayrı başlıkta belirt." + Ortak,
            Array.Empty<AracSecenegi>(), DosyaKabulEder: true),
        new("kod-yaz", "Kod yaz", "terminal", "İstenen işi yapan kod üretir", "Ne yapmasını istiyorsunuz?", "CSV dosyasındaki fiyat sütununun ortalamasını alan bir fonksiyon",
            "Sen kıdemli bir yazılım geliştiricisin. İsteği {dil} dilinde, okunabilir, yorumlu ve hata denetimli kodla çöz. Kodu tek bir kod bloğunda ver, ardından nasıl çalıştırılacağını ve varsayımlarını kısaca açıkla." + Ortak,
            new[] { new AracSecenegi("dil", "Programlama dili", new[] { "C#", "Python", "JavaScript", "TypeScript", "Kotlin", "Java", "SQL", "Go", "PHP" }) }),
        new("fikir", "Fikir üret", "ampul", "Konu hakkında yaratıcı fikirler listeler", "Konu", "Kafe için hafta sonu kampanya fikirleri",
            "Sen yaratıcı bir beyin fırtınası ortağısın. Konu için {adet} özgün ve uygulanabilir fikir üret. Her fikre kısa bir başlık, bir cümle açıklama ve zorluk derecesi (kolay/orta/zor) ekle." + Ortak,
            new[] { new AracSecenegi("adet", "Fikir sayısı", new[] { "5", "10", "15" }) }),
        new("sosyal", "Sosyal medya", "mesaj", "Platforma uygun paylaşım metni yazar", "Paylaşımın konusu", "Yeni açılan butik pastanemiz için açılış duyurusu",
            "Sen bir sosyal medya içerik uzmanısın. {platform} için dikkat çekici, platformun üslubuna uygun 3 farklı paylaşım metni seçeneği yaz. Uygunsa emoji ve en fazla 5 ilgili etiket ekle." + Ortak,
            new[] { new AracSecenegi("platform", "Platform", new[] { "Instagram", "X (Twitter)", "LinkedIn", "Facebook", "TikTok açıklaması" }) }),
        new("ogret", "Konu anlat", "kitap", "Bir konuyu seviyeye uygun öğretir", "Öğrenmek istediğiniz konu", "Fotosentez nasıl çalışır?",
            "Sen iyi bir öğretmensin. Konuyu {seviye} seviyesine uygun, adım adım ve örneklerle anlat. Sonunda 3 soruluk kısa bir kendini test et bölümü ekle (cevaplar en altta)." + Ortak,
            new[] { new AracSecenegi("seviye", "Seviye", new[] { "ortaokul", "lise", "üniversite", "uzman" }) }),
        new("cv", "CV / ön yazı", "belge", "Özgeçmiş maddeleri veya ön yazı hazırlar", "Deneyimleriniz ve hedef pozisyon", "3 yıl Android geliştirici, Kotlin ve .NET MAUI biliyorum; Junior Mobile Developer ilanı",
            "Sen bir kariyer danışmanısın. Verilen bilgilerle {tur} hazırla. Ölçülebilir başarıları öne çıkar, abartma ve uydurma bilgi ekleme; eksik bilgileri [köşeli parantez] ile işaretle." + Ortak,
            new[] { new AracSecenegi("tur", "Belge türü", new[] { "güçlü CV maddeleri", "ön yazı (cover letter)", "LinkedIn 'Hakkında' metni" }) }),
        new("veri", "Veri çıkar", "tablo", "Metinden tablo veya JSON çıkarır", "Kaynak metin (veya dosya ekleyin)", "Ali 25 yaşında İstanbul'da; Ayşe 31 yaşında Ankara'da yaşıyor.",
            "Sen bir veri ayıklama aracısın. Metindeki yapılandırılabilir bilgileri {bicim} olarak çıkar. Bulunamayan alanları boş bırak, tahmin etme. Yalnızca sonucu ver.",
            new[] { new AracSecenegi("bicim", "Çıktı biçimi", new[] { "Markdown tablo", "JSON", "CSV" }) }, DosyaKabulEder: true)
    };

    public static AracTanimi? Bul(string kod) => Hepsi.FirstOrDefault(a => a.Kod == kod);
}
