# ✅ Test Raporu

## Otomatik testler (`tests/FurkanAi.Tests`, xUnit — internet gerekmez)
| Dosya | Kapsam |
|---|---|
| `SaglayiciTestleri.cs` | SSE okuyucu (yorum, olay adı, çok satırlı veri) · OpenAI gövdesi (sistem, görsel, PDF `file`, metin eki, akış) · uyumlu sunucuda PDF→metin · OpenAI/Gemini/Claude akış parçası çözümü · Gemini rol eşleme ve güvenlik engeli · Claude ardışık rol birleştirme ve kullanıcıyla başlama · Türkçe hata + anahtar maskeleme · anahtarsız istemci reddi · model kaydı |
| `MetinVeDosyaTestleri.cs` | Markdown XSS (script/img/javascript: bağlantı) · başlık/liste/tablo/kod/alıntı · akışta kapanmamış kod bloğu · DOCX metni · basit PDF metni · ek türü ve boyut sınırları · 11 araç ve seçenek yerleştirme · hafıza doğrulama/öneri çözümleme · dışa aktarma gidiş-dönüş |

Toplam 21 test metodu (teori satırlarıyla 23 durum).

## Elle test listesi (cihazda, gerçek anahtarla)
- [ ] Her sağlayıcı: bağlantı testi, modelleri getir, akışlı yanıt, durdur, yeniden üret
- [ ] Yanlış anahtar → "API anahtarı geçersiz" · uçak modu → "İnternet bağlantısı yok"
- [ ] Görsel + soru (vision) · PDF özetleme · DOCX özetleme
- [ ] Görsel üretimi (OpenAI, Gemini) → galeri → kaydet/paylaş
- [ ] Mikrofon (cihaz/OpenAI) · sesli okuma (cihaz/OpenAI)
- [ ] Hafıza: ekle, devre dışı bırak, öneri onayla; sohbete etkisini gözlemle
- [ ] Dışa aktar (MD/TXT/JSON) → JSON içe aktar
- [ ] PIN + parmak izi, otomatik kilit, "PIN'imi unuttum"
- [ ] Uygulama arka plandayken uzun yanıt → bildirim → dokununca sohbete gider
- [ ] Geçmiş kaydı kapalı (gizli mod)
