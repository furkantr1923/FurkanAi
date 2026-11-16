# 🐙 FURKAN AI — GitHub'a Yükleme Rehberi (Visual Studio ile, komut satırı yok)

> Bilgisayarınızda ayrı bir **git** kurulu olmasa da olur: Visual Studio kendi Git'ini içinde taşır. Tüm adımlar menülerle yapılır.

## ✅ 0. Yüklemeden önce güvenlik kontrolü (2 dk)
- [ ] Proje klasöründe **`.gitignore`** dosyası var (zip'te hazır). `bin/`, `obj/`, `*.apk`, `*.keystore`, `.env` gibi dosyaları dışlar.
- [ ] Hiçbir dosyada parola, API anahtarı, telefon numarası veya e-posta yok.
- [ ] İmzalama anahtarı (`.keystore` / `.jks`) proje klasörünün **dışında** saklanıyor.
- **API anahtarlarınızı asla** koda, README'ye, issue'ya, ekran görüntüsüne veya commit mesajına yazmayın. Anahtarlar yalnızca uygulama içinden girilir.
- Yanlışlıkla bir anahtar yüklediyseniz: dosyadan silmek **yetmez** — sağlayıcı panelinden anahtarı iptal edip yenisini oluşturun.
- GitHub › Settings › Code security › **Secret scanning** ve **Push protection** seçeneklerini açın; anahtar içeren bir commit'i otomatik engeller.
- Kullanıcı adınız `furkantr1923` değilse `src/FurkanAi.Mobile/Services/Temel.cs` içindeki `GithubDeposu` değerini güncelleyin.

## 1. GitHub hesabı
github.com → **Sign up** (hesabınız varsa giriş yapın). E-postanızı gizli tutmak için: Settings › Emails › **Keep my email addresses private** ✅.

## 2. Visual Studio'da GitHub hesabını bağlayın
**Dosya › Hesap Ayarları…** (File › Account Settings) → **Ekle › GitHub** → tarayıcıda izin verin.

## 3. Depoyu oluşturup yükleyin
1. `FurkanAi.sln` dosyasını Visual Studio'da açın.
2. Üst menü **Git › Git Deposu Oluştur…** (Git › Create Git Repository).
3. Sol tarafta **GitHub** seçili olsun:
   - **Hesap:** GitHub hesabınız
   - **Depo adı:** `FURKAN-AI`
   - **Açıklama:** aşağıdaki "About" metni
   - **Özel depo (Private):** önce işaretli bırakın, kontrol ettikten sonra herkese açabilirsiniz.
4. **Oluştur ve Gönder** (Create and Push) → birkaç saniye bekleyin. ✔️

## 4. GitHub sayfasını düzenleyin
Depo sayfasında sağdaki ⚙️ (About) simgesi:
- **Description:** OpenAI, Gemini, Claude ve OpenAI uyumlu sunucularla çalışan kişisel yapay zekâ asistanı: akışlı sohbet, dosya/görsel analizi, ses, 11 araç, denetimli hafıza. Android · .NET MAUI.
- **Topics:** `ai-assistant, chatbot, openai, gemini, claude, ollama, dotnet-maui, blazor-hybrid, android, llm, csharp, turkish`
- README otomatik görünür. Ekran görüntülerini `docs/ekran-goruntuleri/` klasörüne ekleyip README'ye bağlayabilirsiniz.

## 5. APK'yı "Release" olarak yayınlayın
1. APK'yı üretin: `docs/` içindeki kurulum/APK rehberi (Release › Publish › Ad Hoc).
2. GitHub depo sayfası › **Releases › Create a new release**.
3. **Tag:** `v1.0.0` · **Başlık:** `FURKAN AI 1.0.0` · Açıklama: `CHANGELOG.md`'den kopyalayın.
4. APK dosyasını sürükleyip bırakın → **Publish release**.
> APK'yı kod deposuna değil, yalnızca **Releases**'e ekleyin.

## 6. Sonraki değişiklikleri göndermek
1. **Görünüm › Git Değişiklikleri** (View › Git Changes).
2. Değişen dosyaları kontrol edin (gizli bilgi olmasın!).
3. Kısa bir mesaj yazın (ör. `Ayarlar ekranına yazı boyutu eklendi`) → **Tümünü Kaydet** (Commit All) → **Gönder** (Push ↑).

## 7. Depoyu herkese açmak
Settings › General › en altta **Danger Zone › Change visibility › Public**. Öncesinde 0. adımdaki kontrolü bir kez daha yapın.

## ❓ Sorun giderme
| Sorun | Çözüm |
|---|---|
| "Git Deposu Oluştur" menüsü yok | Visual Studio Installer › Değiştir › Tek tek bileşenler › **Git for Windows** ✅ |
| Gönderme (push) reddedildi | Dosya › Hesap Ayarları'ndan GitHub'dan çıkıp tekrar giriş yapın |
| Push protection anahtar buldu | O satırı silin, anahtarı sağlayıcıda iptal edin, değişikliği tekrar kaydedin |
| Yanlışlıkla `bin/obj` yüklendi | `.gitignore` dosyasının kök klasörde olduğundan emin olun, klasörleri silip tekrar commit edin |
