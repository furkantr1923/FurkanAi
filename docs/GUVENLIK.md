# 🛡️ Güvenlik

- **API anahtarları:** yalnızca `SecureStorage` (Android Keystore ile şifreli). `Preferences`, SQLite, günlük, dışa aktarma ve kaynak kodda yoktur. Gemini anahtarı URL'ye değil başlığa yazılır (sunucu günlüklerinde görünmesin diye).
- **Anahtar maskeleme:** sağlayıcı hata metinlerinde anahtar benzeri diziler (`sk-…`, `AIza…`, `sk-ant-…`) maskelenir.
- **Markdown güvenliği:** model çıktısı önce HTML-kaçışlanır; ham HTML/JS çalıştırılamaz, yalnızca `http/https/mailto` bağlantılara izin verilir (XSS testleri `MetinVeDosyaTestleri`).
- **Hafıza:** parola/anahtar/token benzeri içerik reddedilir; model kendi kendine hafızaya yazamaz.
- **Dosyalar:** gönderimden önce kullanıcı onayı; boyut sınırları (görsel 5 MB, dosya 20 MB).
- **Kilit:** PIN = 16 bayt tuz + PBKDF2-SHA256 (100.000 tur), sabit zamanlı karşılaştırma, 5 hatadan sonra artan bekleme; isteğe bağlı BiometricPrompt.
- **Ağ:** OpenAI/Gemini/Claude HTTPS zorunlu. `usesCleartextTraffic` yalnızca yerel ağdaki OpenAI uyumlu sunucular (http://192.168…) için açıktır; uygulama resmî sağlayıcılar için http adresini reddeder.
- **Yedek:** `allowBackup=false` — sohbetler Google bulut yedeğine gitmez.

## Anahtar sızarsa
Silmek yetmez: sağlayıcı panelinden anahtarı **iptal edin (revoke)** ve yenisini oluşturun.

## Açık bildirimi
GitHub › **Security › Report a vulnerability** (özel bildirim) kullanın.
