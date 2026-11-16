# 📱 Desteklenen Android sürümleri

| Sürüm | API | Durum |
|---|---|---|
| Android 8.0 – 8.1 | 26–27 | ✅ Destekleniyor (biyometrik kilit yok, yalnızca PIN) |
| Android 9 | 28 | ✅ Biyometrik kilit (parmak izi) |
| Android 10 | 29 | ✅ Destekleniyor |
| Android 11 – 12L | 30–32 | ✅ |
| Android 13 – 16 | 33–36 | ✅ Bildirim izni çalışma anında istenir |

**Neden en az API 26?** .NET 10 MAUI için pratik alt sınır; Android 8 altı cihaz payı çok düşüktür.

**Hedef API**: .NET 10 Android iş yükünün varsayılanı (Google Play'in güncel hedef API şartını karşılar).
