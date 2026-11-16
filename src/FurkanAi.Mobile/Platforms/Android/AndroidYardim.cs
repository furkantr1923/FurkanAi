using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Biometrics;
using Android.Media;
using Android.OS;
using Android.Speech;
using AndroidX.Core.App;
using AUri = Android.Net.Uri;

namespace FurkanAi.Mobile.Platforms.Android;

internal static class Baglam
{
    public static Context Uygulama => global::Android.App.Application.Context;
    public static Activity Etkinlik => FurkanAi.Mobile.MainActivity.Aktif ?? Platform.CurrentActivity ?? throw new InvalidOperationException("Etkinlik yok.");
}

/// <summary>Arka planda tamamlanan yanıtlar için bildirim.</summary>
public static class AndroidBildirim
{
    public const string Kanal = "fa_yanit";

    public static void KanalOlustur()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;
        var nm = (NotificationManager?)Baglam.Uygulama.GetSystemService(Context.NotificationService);
        nm?.CreateNotificationChannel(new NotificationChannel(Kanal, "Yanıt bildirimleri", NotificationImportance.Default) { Description = "Uygulama arka plandayken tamamlanan yapay zekâ yanıtları" });
    }

    public static void Goster(int id, string baslik, string metin, long sohbetId)
    {
        var ctx = Baglam.Uygulama;
        var intent = ctx.PackageManager?.GetLaunchIntentForPackage(ctx.PackageName!);
        if (intent == null) return;
        intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        intent.PutExtra("fa_sohbet", sohbetId);
        var pi = PendingIntent.GetActivity(ctx, id, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
        var b = new NotificationCompat.Builder(ctx, Kanal)
            .SetSmallIcon(Resource.Drawable.fa_bildirim).SetColor(unchecked((int)0xFF8B5CFF))
            .SetContentTitle(baslik).SetContentText(metin).SetStyle(new NotificationCompat.BigTextStyle().BigText(metin))
            .SetAutoCancel(true).SetContentIntent(pi);
        try { NotificationManagerCompat.From(ctx).Notify(id, b.Build()); } catch (Java.Lang.SecurityException) { }
    }
}

/// <summary>Parmak izi / yüz doğrulama (Android 9+ BiometricPrompt, harici paket gerekmez).</summary>
public static class AndroidBiyometri
{
    public static bool Destekleniyor()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(29)) return OperatingSystem.IsAndroidVersionAtLeast(28) &&
            Baglam.Uygulama.PackageManager!.HasSystemFeature(global::Android.Content.PM.PackageManager.FeatureFingerprint);
        var bm = (BiometricManager?)Baglam.Uygulama.GetSystemService(Context.BiometricService);
        if (bm == null) return false;
#pragma warning disable CA1422
        return OperatingSystem.IsAndroidVersionAtLeast(30)
            ? bm.CanAuthenticate((int)BiometricManagerAuthenticators.BiometricWeak) == BiometricCode.Success
            : bm.CanAuthenticate() == BiometricCode.Success;
#pragma warning restore CA1422
    }

    public static Task<bool> Dogrula(string baslik, string alt)
    {
        var tcs = new TaskCompletionSource<bool>();
        if (!OperatingSystem.IsAndroidVersionAtLeast(28)) { tcs.SetResult(false); return tcs.Task; }
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var etkinlik = Baglam.Etkinlik;
                var yurutucu = etkinlik.MainExecutor!;
                var b = new BiometricPrompt.Builder(etkinlik).SetTitle(baslik).SetSubtitle(alt)
                    .SetNegativeButton("PIN kullan", yurutucu, new DialogDinleyici(() => tcs.TrySetResult(false)));
                var iptal = new CancellationSignal();
                b.Build().Authenticate(iptal, yurutucu, new Geri(tcs));
            }
            catch (Exception e) { Services.HataKaydi.Yaz("Biyometri", e); tcs.TrySetResult(false); }
        });
        return tcs.Task;
    }

    private sealed class Geri : BiometricPrompt.AuthenticationCallback
    {
        private readonly TaskCompletionSource<bool> _t;
        public Geri(TaskCompletionSource<bool> t) => _t = t;
        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult? r) => _t.TrySetResult(true);
        public override void OnAuthenticationError(BiometricErrorCode kod, Java.Lang.ICharSequence? m) => _t.TrySetResult(false);
        // OnAuthenticationFailed: tek hatalı deneme; sistem penceresi açık kalır, tekrar denenebilir
    }

    private sealed class DialogDinleyici : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        private readonly Action _a;
        public DialogDinleyici(Action a) => _a = a;
        public void OnClick(IDialogInterface? d, int w) => _a();
    }
}

/// <summary>Cihazın konuşma tanıma ekranı (Google/Samsung vb.). İzin gerektirmez; sistem kendi arayüzünü gösterir.</summary>
public static class AndroidKonusma
{
    public static bool Var() => SpeechRecognizer.IsRecognitionAvailable(Baglam.Uygulama);

    public static async Task<string?> Dinle(string dil)
    {
        var i = new Intent(RecognizerIntent.ActionRecognizeSpeech);
        i.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
        i.PutExtra(RecognizerIntent.ExtraLanguage, dil);
        i.PutExtra(RecognizerIntent.ExtraPrompt, "Konuşun…");
        i.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
        var (sonuc, veri) = await FurkanAi.Mobile.MainActivity.SonucIcinBaslat(i);
        if (sonuc != Result.Ok) return null;
        return veri?.GetStringArrayListExtra(RecognizerIntent.ExtraResults)?.FirstOrDefault();
    }
}

/// <summary>Mikrofon kaydı (AAC/M4A) — OpenAI ses→yazı için.</summary>
public static class AndroidSesKaydi
{
    private static MediaRecorder? _k;
    private static string? _dosya;
    public static bool Kayitta => _k != null;

    public static void Baslat()
    {
        Durdur(sil: true);
        _dosya = System.IO.Path.Combine(FileSystem.CacheDirectory, $"kayit_{DateTime.Now:HHmmss}.m4a");
#pragma warning disable CA1422
        _k = OperatingSystem.IsAndroidVersionAtLeast(31) ? new MediaRecorder(Baglam.Uygulama) : new MediaRecorder();
#pragma warning restore CA1422
        _k.SetAudioSource(AudioSource.Mic);
        _k.SetOutputFormat(OutputFormat.Mpeg4);
        _k.SetAudioEncoder(AudioEncoder.Aac);
        _k.SetAudioSamplingRate(16000);
        _k.SetAudioEncodingBitRate(64000);
        _k.SetMaxDuration(120_000);
        _k.SetOutputFile(_dosya);
        _k.Prepare();
        _k.Start();
    }

    /// <summary>Kaydı bitirir ve dosya yolunu döndürür (çok kısa/başarısızsa null).</summary>
    public static string? Durdur(bool sil = false)
    {
        var k = _k; _k = null;
        if (k == null) return null;
        try { k.Stop(); } catch { sil = true; }
        k.Release();
        if (sil && _dosya != null) { try { File.Delete(_dosya); } catch { } return null; }
        return _dosya;
    }
}

/// <summary>Sağlayıcıdan gelen mp3 sesini çalar.</summary>
public static class AndroidSesCalar
{
    private static MediaPlayer? _p;

    public static Task Cal(byte[] mp3, CancellationToken ct)
    {
        Durdur();
        var yol = System.IO.Path.Combine(FileSystem.CacheDirectory, "okuma.mp3");
        File.WriteAllBytes(yol, mp3);
        var tcs = new TaskCompletionSource();
        var p = new MediaPlayer();
        _p = p;
        p.SetDataSource(yol);
        p.Completion += (_, _) => { tcs.TrySetResult(); Durdur(); };
        p.Error += (_, _) => { tcs.TrySetResult(); Durdur(); };
        ct.Register(() => { Durdur(); tcs.TrySetCanceled(); });
        p.Prepare();
        p.Start();
        return tcs.Task;
    }

    public static void Durdur()
    {
        var p = _p; _p = null;
        if (p == null) return;
        try { if (p.IsPlaying) p.Stop(); } catch { }
        p.Release();
    }
}

public static class AndroidDosya
{
    /// <summary>"Farklı kaydet" penceresi açar. Kullanıcı iptal ederse false döner.</summary>
    public static async Task<bool> Kaydet(string ad, string mime, byte[] veri)
    {
        var i = new Intent(Intent.ActionCreateDocument);
        i.AddCategory(Intent.CategoryOpenable);
        i.SetType(mime);
        i.PutExtra(Intent.ExtraTitle, ad);
        var (sonuc, data) = await FurkanAi.Mobile.MainActivity.SonucIcinBaslat(i);
        if (sonuc != Result.Ok || data?.Data is not AUri uri) return false;
        await using var akis = Baglam.Uygulama.ContentResolver!.OpenOutputStream(uri, "wt") ?? throw new IOException("Dosya yazılamadı.");
        await akis.WriteAsync(veri);
        return true;
    }
}

/// <summary>Büyük fotoğrafları sağlayıcı sınırlarına uygun boyuta küçültür (en uzun kenar 2048 px, JPEG).</summary>
public static class AndroidGorsel
{
    public static (byte[] Veri, string Mime) Kucult(byte[] veri, string mime, int azamiKenar = 2048, long azamiBayt = 4_500_000)
    {
        var secenek = new BitmapFactory.Options { InJustDecodeBounds = true };
        BitmapFactory.DecodeByteArray(veri, 0, veri.Length, secenek);
        var en = Math.Max(secenek.OutWidth, secenek.OutHeight);
        if (en <= 0) throw new InvalidDataException("Görsel çözülemedi.");
        var desteklenen = mime is "image/jpeg" or "image/png" or "image/webp" or "image/gif";
        if (en <= azamiKenar && veri.LongLength <= azamiBayt && desteklenen) return (veri, mime);
        var ornek = 1;
        while (en / (ornek * 2) >= azamiKenar) ornek *= 2;
        using var bmp = BitmapFactory.DecodeByteArray(veri, 0, veri.Length, new BitmapFactory.Options { InSampleSize = ornek })
                        ?? throw new InvalidDataException("Görsel çözülemedi.");
        var oran = Math.Min(1.0, azamiKenar / (double)Math.Max(bmp.Width, bmp.Height));
        using var olcekli = oran < 1 ? Bitmap.CreateScaledBitmap(bmp, (int)(bmp.Width * oran), (int)(bmp.Height * oran), true)! : bmp;
        using var ms = new MemoryStream();
        var kalite = 88;
        do { ms.SetLength(0); olcekli.Compress(Bitmap.CompressFormat.Jpeg!, kalite, ms); kalite -= 12; } while (ms.Length > azamiBayt && kalite > 30);
        return (ms.ToArray(), "image/jpeg");
    }
}
