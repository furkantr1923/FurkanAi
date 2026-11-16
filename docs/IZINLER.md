# 🔑 İzinler

| İzin | Ne zaman | Neden |
|---|---|---|
| `INTERNET`, `ACCESS_NETWORK_STATE` | otomatik | Sağlayıcılara bağlanmak, bağlantı durumunu göstermek |
| `RECORD_AUDIO` | OpenAI ses→yazı ile mikrofona basınca | Ses kaydı ("Cihaz" motoru izin istemez; sistem ekranı kullanılır) |
| `CAMERA` | Ek › Kamera seçilince | Fotoğraf çekip eklemek |
| `POST_NOTIFICATIONS` (13+) | Yanıt bildirimi açılınca | Arka planda biten yanıtlar |
| `USE_BIOMETRIC` | Biyometrik kilit açılınca | Parmak izi/yüz ile kilit açma |
| `READ_EXTERNAL_STORAGE` (≤ 9) | Dosya/galeri seçimi | Eski Android sürümlerinde dosya okuma |
