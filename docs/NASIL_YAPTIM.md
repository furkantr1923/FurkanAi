# 🗣️ "FURKAN AI'ı nasıl yaptın?" — Hazır cevaplar

> Bu metinler dürüst olacak şekilde yazıldı: projede yapay zekâ yardımı kullanıldığını gizlemez, sizin yaptığınız işi de net anlatır.

## ⚡ Kısa cevap (sohbet / mesaj)
"FURKAN AI, OpenAI, Gemini ve Claude ile çalışan kişisel bir yapay zekâ asistanı. Özellikleri ve kuralları ben belirledim; kodun büyük kısmını yapay zekâ asistanı (Claude) ile birlikte yazdım. Projeyi Visual Studio'da derledim, telefonumda test ettim ve GitHub'da yayınladım. C# ve .NET MAUI ile yazıldı."

## 📝 Orta uzunlukta (LinkedIn / README / sunum açılışı)
"FURKAN AI benim kişisel projem: OpenAI, Gemini ve Claude ile çalışan kişisel bir yapay zekâ asistanı.
Önce ne istediğimi ayrıntılı bir gereksinim listesine döktüm: özellikler, güvenlik kuralları (şifre ve API anahtarlarının asla koda girmemesi gibi), tasarım dili ve test beklentileri.
Ardından yapay zekâ asistanıyla birlikte çalışarak projeyi parça parça oluşturduk: önce platformdan bağımsız iş mantığı ve birim testleri, sonra arayüz ekranları, en son belgeler.
Kodu Visual Studio 2026'da derleyip Android telefonumda denedim, çıkan derleme hatalarını düzelttim ve APK'yı GitHub Releases'te yayınladım.
Bu süreçte .NET MAUI Blazor Hybrid mimarisini, SQLite ile yerel veri saklamayı, güvenli depolamayı ve GitHub ile sürüm yönetimini öğrendim."

## 🎓 Ayrıntılı (hoca / mülakat / jüri)
**Fikir ve planlama:** Neden bu uygulamaya ihtiyaç duyduğumu ve hangi özelliklerin şart olduğunu yazdım. Reklam/hesap zorunluluğu olmaması, verilerin cihazda kalması ve gizli bilgilerin kodda bulunmaması temel kurallarımdı.

**Geliştirme yöntemi:** Yapay zekâyı bir "eş programcı" gibi kullandım. Ben gereksinimleri ve kararları verdim; asistan kodu ve testleri yazdı; ben derleyip denedim ve hataları geri bildirdim. Her özelliği "çalışıyormuş gibi görünen" değil gerçekten çalışan şekilde istemek ve desteklenmeyen durumlarda kullanıcıya açık mesaj gösterilmesi bilinçli bir tercihti.

**Mimari:** Çözüm üç projeden oluşuyor: platformdan bağımsız **Core** kütüphanesi (iş mantığı), **Mobile** uygulaması (.NET MAUI Blazor Hybrid arayüz + Android entegrasyonu) ve **Tests** (xUnit birim testleri).

**Benim katkım:** Gereksinim analizi, özellik ve güvenlik kararları, Visual Studio'da derleme, hata ayıklama, cihazda test, APK imzalama ve GitHub yayını.

## 🔧 Teknik sorulara cevaplar
**Neyle yazdın?**
C# ve .NET 10 MAUI Blazor Hybrid ile. Sağlayıcı istemcileri, akış okuyucu, Markdown çevirici ve dosya okuyucu harici paket kullanmadan Core kütüphanesinde yazıldı.

**Yanıtlar nasıl canlı akıyor?**
Sağlayıcıların 'stream' modu Server-Sent Events gönderiyor; uygulama bu akışı satır satır okuyup her parçayı ekrana ekliyor. Durdur düğmesi isteği iptal ediyor.

**API anahtarları nerede?**
Telefonun güvenli deposunda (Android Keystore ile şifreli). Kodda, veritabanında, günlükte ve dışa aktarmada yok.

**Hafıza nasıl çalışıyor?**
Sadece benim onayladığım kısa bilgiler her sohbetin başında modele hatırlatılıyor. Model kendi kendine hafızaya bir şey yazamıyor.

**Dosyaları nasıl okuyor?**
Görseller ve PDF'ler destekleyen modellere doğrudan gidiyor; DOCX ve metin dosyalarının metni uygulamada çıkarılıyor. Göndermeden önce onay soruluyor.

**Yapay zekâ mı yazdı?**
Evet, kodun büyük kısmı yapay zekâ yardımıyla yazıldı; bunu gizlemiyorum. Ne istediğimi tanımlamak, sonuçları denetlemek, derlemek, test etmek ve yayınlamak benim işimdi. Bugün birçok yazılımcı da benzer araçlarla çalışıyor; önemli olan ne yaptığını anlamak.

**Zorlandığın kısım?**
Derleme sırasında çıkan hataları (eksik `using`, paket sürümleri gibi) anlayıp düzeltmek ve uygulamayı gerçek cihazda test etmek. Hata mesajını okuyup kaynağını bulmayı öğrendim.

**Bundan sonra ne ekleyeceksin?**
Kullanıcı geri bildirimlerine göre yeni özellikler, daha fazla test ve Google Play yayını.

---
💡 İpucu: "Tamamen tek başıma yazdım" demeyin — sorulduğunda yapay zekâ yardımını açıkça söylemek hem dürüst hem de daha güven verici.
