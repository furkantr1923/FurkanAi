# 🧱 Mimari

```
┌──────────────────────────── FurkanAi.Mobile (net10.0-android) ────────────────────────────┐
│ Blazor sayfaları: Ana sayfa · Sohbet · Sohbetler · Araçlar · Görsel · Hafıza · Sağlayıcılar │
│                  Ayarlar · Güvenlik · Yardım · Gizlilik · Hakkında                         │
│ Servisler: SohbetServisi (akış, bağlam, hafıza, bildirim) · SaglayiciServisi (SecureStorage)│
│            SohbetDeposu (SQLite) · SesServisi (STT/TTS) · KilitServisi (PIN+biyometri)      │
│ Android:   BiometricPrompt · RecognizerIntent · MediaRecorder/MediaPlayer · bildirim · SAF  │
└───────────────────────────────────────────────┬────────────────────────────────────────────┘
                                                ▼
┌──────────────────────────── FurkanAi.Core (net10.0, harici paket yok) ─────────────────────┐
│ Saglayicilar/ SaglayiciIstemcisi (taban) → OpenAi · Gemini · Claude  ·  SseOkuyucu          │
│               ModelKaydi (merkezi kayıt + yetenek tahmini) · SaglayiciHatasi (TR mesajlar)  │
│ Metin/        MarkdownCizici (önce kaçışla, sonra biçimle; yalnızca http/https/mailto)       │
│ Dosyalar/     DosyaOkuyucu: tür tespiti, sınırlar, TXT kodlama, DOCX (OpenXML), PDF (Flate)   │
│ Araclar/      11 araç = sistem istemi şablonu + seçenekler                                  │
│ Hafiza/       doğrulama (anahtar/parola reddi), sistem bloğu, öneri istemi ve çözümleyici     │
│ Disa/         JSON / TXT / Markdown                                                         │
└────────────────────────────────────────────────────────────────────────────────────────────┘
```

## Akış (bir mesaj gönderildiğinde)
1. `SohbetSayfasi` → ek onayı → `SohbetServisi.Gonder` mesajı ve ekleri kaydeder.
2. `Calistir`: sistem istemi (temel + kişisel talimat + yanıt dili + **etkin hafıza** + araç istemi) ve son *N* mesaj hazırlanır; ekler yalnızca son 6 mesajda tam gönderilir.
3. İstemci `IAsyncEnumerable<string>` ile parça parça akış verir; 50 ms'de bir arayüz güncellenir (sayfa kapansa da akış sürer).
4. Bitince yanıt kaydedilir; uygulama arka plandaysa bildirim, ayar açıksa sesli okuma.
5. Hata/iptal: kısmi metin korunur, hata Türkçe açıklamayla ayrı balonda gösterilir; "Yeniden üret" ile tekrar denenir.

## Veri
SQLite tabloları: `Sohbetler`, `Mesajlar` (ek meta verisi JSON), `Hafiza`, `Gorseller`. Ek ve görsel dosyaları uygulamanın özel klasöründe; sohbet silinince ekleri de silinir.
