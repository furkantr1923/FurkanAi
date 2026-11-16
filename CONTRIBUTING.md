# 🤝 Katkı Rehberi

Katkılarınız memnuniyetle karşılanır! Lütfen şu adımları izleyin:

1. Bir **issue** açarak değişikliği kısaca anlatın (hata bildirimi için: Android sürümü, sağlayıcı, model, adımlar, hata günlüğü — **API anahtarı paylaşmayın**).
2. Depoyu fork'layın, `ozellik/kisa-ad` adında bir dal açın.
3. Kod stili: `.editorconfig` · C# 13 · Türkçe adlandırma (mevcut kodla tutarlı) · `Nullable` açık.
4. İş mantığını mümkünse `FurkanAi.Core` içine koyun ve **test** ekleyin (`tests/FurkanAi.Tests`).
5. Razor'da kod bağlamında `@if` yerine `if` kullanın; kullanıcı girdisini asla `MarkupString` ile basmayın (Markdown yalnızca `MarkdownCizici` üzerinden).
6. Yeni bir sağlayıcı eklerken: `SaglayiciIstemcisi`'nden türetin, `GovdeOlustur`/`DeltaCoz` fonksiyonlarını saf (test edilebilir) yazın, `ModelKaydi`'na ekleyin.
7. Pull request açıklamasında neyi, neden değiştirdiğinizi ve nasıl test ettiğinizi yazın.

### Güvenlik kuralları (zorunlu)
- API anahtarı, parola, `.keystore` dosyası **asla** commit edilmez.
- Anahtarlar yalnızca `SaglayiciServisi` → `SecureStorage` üzerinden okunur/yazılır.
- Hata günlüğüne sohbet içeriği yazılmaz.

### Davranış kuralları
Saygılı, yapıcı ve kapsayıcı olun.
