# 🔌 Sağlayıcı Kurulumu

| Sağlayıcı | Anahtar | Varsayılan adres | Varsayılan model* | Görsel girdi | PDF | Görsel üretim |
|---|---|---|---|---|---|---|
| OpenAI | platform.openai.com › API keys | `https://api.openai.com/v1` | `gpt-5-mini` | ✅ | ✅ | ✅ `gpt-image-1` |
| Google Gemini | aistudio.google.com › Get API key | `https://generativelanguage.googleapis.com/v1beta` | `gemini-2.5-flash` | ✅ | ✅ | ✅ `gemini-2.5-flash-image` |
| Anthropic Claude | console.anthropic.com › API Keys | `https://api.anthropic.com/v1` | `claude-sonnet-5-5` | ✅ | ✅ | ❌ |
| OpenAI uyumlu | (isteğe bağlı) | sizin adresiniz | `llama3.2` | modele bağlı | metin olarak | ❌ |

\* Model adları sık değişir. Uygulamadaki **Modelleri getir** düğmesi hesabınızda erişilebilen güncel listeyi çeker; listede olmayan modeli elle yazabilirsiniz.

## Kullanılan API'ler
- **OpenAI:** `POST /chat/completions` (stream), `GET /models`, `POST /images/generations`, `POST /audio/transcriptions`, `POST /audio/speech` — `Authorization: Bearer`.
- **Gemini:** `POST /models/{model}:streamGenerateContent?alt=sse`, `GET /models`, `:generateContent` (görsel) — anahtar `x-goog-api-key` başlığında (URL'de değil).
- **Claude:** `POST /messages` (stream), `GET /models` — `x-api-key`, `anthropic-version: 2023-06-01`.
- **OpenAI uyumlu:** OpenAI ile aynı uç noktalar, sizin adresinizde.

## Örnek: Ollama (yerel, ücretsiz)
1. Bilgisayara Ollama kurun, `ollama pull llama3.2`.
2. Ortam değişkeni `OLLAMA_HOST=0.0.0.0` → Ollama'yı yeniden başlatın.
3. Bilgisayarın yerel IP'sini öğrenin (ör. `192.168.1.20`).
4. FURKAN AI › Sağlayıcılar › OpenAI uyumlu → adres `http://192.168.1.20:11434/v1` → Modelleri getir → Kaydet.
