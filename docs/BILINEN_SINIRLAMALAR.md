# ⚠️ Bilinen Sınırlamalar

- **Derleme:** Proje Visual Studio 2026 + .NET 10 MAUI için yazıldı; bu ortamda derlenmedi. Derleme hatası olursa hata listesindeki dosya/satırı paylaşın.
- **Model adları değişir:** Varsayılan model listesi başlangıç önerisidir; "Modelleri getir" ile güncelleyin. Elle yazılan modellerin yetenekleri (görsel/PDF) adından **tahmin** edilir.
- **PDF metin çıkarma** en iyi çaba yöntemidir: taranmış PDF'ler ve özel yazı tipi kodlamaları okunamayabilir. OpenAI/Gemini/Claude PDF'i doğrudan okuduğu için bu yalnızca OpenAI uyumlu sunucuları etkiler.
- **Bağlam:** Son *N* mesaj gönderilir; ekler yalnızca son 6 mesajda tam gönderilir (maliyet ve sınırlar). Çok uzun sohbetlerde model eski ayrıntıları "unutabilir".
- **Akış arka planda:** Android uygulamayı uzun süre arka planda tutarsa sistem süreci durdurabilir; bu durumda yanıt yarım kalabilir.
- **Ses:** "Cihaz" konuşma tanıma, telefonda bir tanıma servisi (çoğunlukla Google) gerektirir. Cihaz TTS kalitesi yüklü ses paketine bağlıdır.
- **Görsel üretimi** yalnızca OpenAI ve Gemini'de; Claude görsel üretmez. Bazı modeller hesap doğrulaması gerektirebilir.
- **Araç kullanımı (function calling), web araması ve dosya arama** yoktur; model internette arama yapmaz.
- **Yalnızca Android** hedeflenmiştir.
- **Arayüz dili** yalnızca Türkçedir (yanıt dili ayarlanabilir).
