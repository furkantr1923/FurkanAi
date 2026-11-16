# ✨ FURKAN AI

> OpenAI, Google Gemini, Anthropic Claude veya kendi sunucunuzla (Ollama, LM Studio, OpenRouter…) çalışan **kişisel yapay zekâ asistanı**.
> Android · .NET MAUI Blazor Hybrid · Türkçe · Hesapsız · Reklamsız

![Platform](https://img.shields.io/badge/platform-Android%208%2B-3DDC84) ![.NET](https://img.shields.io/badge/.NET-10%20MAUI-512BD4) ![Lisans](https://img.shields.io/badge/lisans-MIT-blue)

> ⚠️ Bağımsız, resmî olmayan bir projedir. OpenAI, Google veya Anthropic ile bağlantısı yoktur; marka adları yalnızca uyumluluğu belirtmek için kullanılır.

## ✨ Özellikler

| Alan | Neler var |
|---|---|
| 🔌 **Sağlayıcılar** | OpenAI · Gemini · Claude · OpenAI uyumlu (özel adres) · merkezi model kaydı · "Modelleri getir" · bağlantı testi · sağlayıcı/model sohbet içinden değiştirilebilir |
| 🔐 **Anahtar güvenliği** | API anahtarları yalnızca `SecureStorage` (Android Keystore) içinde; kodda, günlükte, dışa aktarımda ve GitHub'da **yok** · hata mesajlarında anahtar maskeleme |
| 💬 **Sohbet** | Canlı akış (SSE) · durdur · kopyala · yeniden üret · paylaş · mesajı düzenle · Markdown (başlık, liste, tablo, alıntı) · dil etiketli kod blokları + "Kopyala" · sohbet arama, yeniden adlandırma, sabitleme, silme |
| 📎 **Dosya analizi** | Görsel (JPG/PNG/WEBP), PDF, DOCX, TXT ve kod dosyaları · göndermeden önce **açık onay** · büyük görseller otomatik küçültülür · PDF'ler destekleyen sağlayıcılara doğrudan gider |
| 🖼️ **Görsel** | Görsel üretimi (OpenAI `gpt-image-1`, Gemini Flash Image) · görsel analizi (vision) · yerel galeri |
| 🎙️ **Ses** | Sesten yazıya: cihaz (Android) veya OpenAI · yazıdan sese: cihaz veya OpenAI — iki yön bağımsız · otomatik sesli okuma |
| 🧰 **11 araç** | Özetle · Çevir · Yazım düzelt · E-posta yaz · Kod açıkla · Kod yaz · Fikir üret · Sosyal medya · Konu anlat · CV/ön yazı · Veri çıkar |
| 🧠 **Denetimli hafıza** | Sohbet geçmişinden ayrı · yalnızca kullanıcının eklediği/onayladığı bilgiler · düzenle, devre dışı bırak, sil · "Hafıza önerisi çıkar" (onaysız kaydetmez) · parola/anahtar benzeri bilgileri reddeder |
| 📤 **Dışa aktarma** | Sohbet başına Markdown/TXT/JSON · tüm sohbetler JSON · JSON'dan içe aktarma |
| ⚙️ **Ayarlar** | Tema (koyu/açık/sistem), 4 vurgu rengi, yazı boyutu · yanıt dili · kişisel talimatlar · sıcaklık, en uzun yanıt, bağlam uzunluğu · geçmişi kaydetme (gizli mod) · bildirimler |
| 🔒 **Kilit** | PIN (PBKDF2 + tuz, deneme sınırı) + parmak izi/yüz (Android 9+) · otomatik kilit |
| 📴 **Çevrimdışı** | Geçmiş sohbetler, hafıza ve galeri internetsiz açılır; internet yoksa açık mesaj · yerel ağdaki Ollama internetsiz çalışır |
| 🧯 **Hata yönetimi** | 401/403/404/413/429/5xx için Türkçe açıklama ve çözüm · zaman aşımı · güvenlik filtresi bildirimi · yarım kalan yanıtlar korunur · yerel hata günlüğü |

**Sahte özellik yok:** Bir yetenek seçili sağlayıcıda yoksa (ör. Claude ile görsel üretimi, anahtar girilmemiş sağlayıcı) uygulama bunu açıkça söyler.

## 🚀 Hızlı başlangıç
1. **Visual Studio 2026** + *.NET Multi-platform App UI development* iş yükü.
2. `FurkanAi.sln` → başlangıç projesi **FurkanAi.Mobile** → telefonu bağlayın → ▶.
3. Uygulamada **Ayarlar › Sağlayıcılar** → anahtarınızı girin → *Bağlantı testi*.
4. APK: [docs/KURULUM.md](docs/KURULUM.md).

## 🧱 Mimari
```
src/FurkanAi.Core     → saf .NET 10 (harici paket yok): sağlayıcı istemcileri, SSE okuyucu, güvenli Markdown→HTML,
                        TXT/DOCX/PDF metin çıkarma, 11 araç, hafıza kuralları, dışa aktarma
src/FurkanAi.Mobile   → MAUI Blazor Hybrid: sayfalar, SQLite, SecureStorage, ses, biyometri, bildirim
tests/FurkanAi.Tests  → xUnit: istek gövdeleri, akış çözümleme, Markdown XSS, dosya okuma, hafıza, dışa aktarma
```
Ayrıntı: [docs/MIMARI.md](docs/MIMARI.md) · Sağlayıcı kurulumu: [docs/SAGLAYICILAR.md](docs/SAGLAYICILAR.md)

## 📚 Belgeler
[Kurulum ve APK](docs/KURULUM.md) · [Sağlayıcılar](docs/SAGLAYICILAR.md) · [Mimari](docs/MIMARI.md) · [Güvenlik](docs/GUVENLIK.md) · [Gizlilik](docs/GIZLILIK.md) · [İzinler](docs/IZINLER.md) · [Android sürümleri](docs/ANDROID_SURUMLERI.md) · [Test raporu](docs/TEST_RAPORU.md) · [Bilinen sınırlamalar](docs/BILINEN_SINIRLAMALAR.md) · [Katkı](CONTRIBUTING.md) · [Değişiklikler](CHANGELOG.md) · [GitHub'a yükleme](docs/GITHUB_YUKLEME.md) · [Nasıl yaptım?](docs/NASIL_YAPTIM.md)

## 🔐 Gizli bilgiler
Depoda hiçbir API anahtarı, parola veya imzalama anahtarı yoktur ve **eklenmemelidir**. Bir anahtarı yanlışlıkla commit ettiyseniz silmek yetmez: sağlayıcı panelinden anahtarı **iptal edip yenisini** oluşturun.

## 📄 Lisans
MIT © 2026 Furkan Baykal
