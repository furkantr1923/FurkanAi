# 🛠️ Kurulum, çalıştırma ve APK

## Gereksinimler
- Windows 10/11 + **Visual Studio 2026** (Community yeterli) · *.NET Multi-platform App UI development* iş yükü
- Android 8.0+ telefon (USB hata ayıklama açık) veya emülatör
- En az bir sağlayıcı: OpenAI / Gemini / Claude API anahtarı **veya** yerel ağda Ollama/LM Studio

## Çalıştırma
1. Zip'i kısa bir yola çıkarın (ör. `C:\Projeler\FurkanAi`).
2. `FurkanAi.sln` → Visual Studio. NuGet paketleri ilk açılışta iner.
3. Başlangıç projesi **FurkanAi.Mobile**, hedef **Android**.
4. Telefonu seçip ▶. İlk derleme 5–10 dk sürebilir.
5. Uygulamada **Ayarlar › Sağlayıcılar** → anahtarı yapıştırın → **Bağlantı testi** → **Kaydet ve varsayılan yap**.

## APK
1. Yapılandırma: **Release**.
2. **FurkanAi.Mobile** sağ tık → **Publish…** → **Ad Hoc** → **Create** ile imzalama anahtarı oluşturun (şifreyi not edin).
3. **Save As** → `.apk` dosyası.

> 🔐 `.keystore` dosyası ve şifresi GitHub'a yüklenmez (`.gitignore`). Kaybederseniz uygulamayı güncelleyemezsiniz; güvenli yerde saklayın.

## Testler
**Test → Test Gezgini → Tümünü çalıştır** (`FurkanAi.Tests`, cihaz gerekmez, internet gerekmez).

## Sık sorunlar
| Belirti | Çözüm |
|---|---|
| `NU1101` | İnternet bağlantısı · NuGet önbelleğini temizle |
| `XA5207` | Android SDK Manager → API 35/36 platformunu kur |
| Ollama'ya bağlanamıyor | Bilgisayarda `OLLAMA_HOST=0.0.0.0`, güvenlik duvarında 11434 portu açık, aynı Wi-Fi |
| "API anahtarı geçersiz" | Anahtarı yeniden kopyalayın; sağlayıcı panelinde etkin ve krediniz var mı kontrol edin |
