using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using FurkanAi.Mobile.Services;

namespace FurkanAi.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, Exported = true, WindowSoftInputMode = Android.Views.SoftInput.AdjustResize,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "text/plain", Label = "FURKAN AI'a sor")]
public class MainActivity : MauiAppCompatActivity
{
    private static readonly Dictionary<int, TaskCompletionSource<(Result Sonuc, Intent? Veri)>> _bekleyenler = new();
    private static int _istekNo = 5000;
    public static MainActivity? Aktif { get; private set; }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Aktif = this;
        Platforms.Android.AndroidBildirim.KanalOlustur();
        IntentIsle(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        IntentIsle(intent);
    }

    protected override void OnDestroy()
    {
        if (Aktif == this) Aktif = null;
        base.OnDestroy();
    }

    /// <summary>Bildirimden (sohbete git) ve "Paylaş → FURKAN AI'a sor" ile gelen istekleri yönlendirir.</summary>
    private static void IntentIsle(Intent? intent)
    {
        if (intent == null) return;
        if (intent.Action == Intent.ActionSend && intent.Type == "text/plain" && intent.GetStringExtra(Intent.ExtraText) is { Length: > 0 } metin)
        {
            Yonlendirme.Iste("sohbet/0?metin=" + Uri.EscapeDataString(metin.Length > 8000 ? metin[..8000] : metin));
            intent.RemoveExtra(Intent.ExtraText);
            return;
        }
        var sohbet = intent.GetLongExtra("fa_sohbet", 0);
        if (sohbet > 0) { intent.RemoveExtra("fa_sohbet"); Yonlendirme.Iste("sohbet/" + sohbet); }
    }

    /// <summary>Sonuç bekleyen bir etkinlik başlatır (konuşma tanıma, dosya kaydetme).</summary>
    public static Task<(Result Sonuc, Intent? Veri)> SonucIcinBaslat(Intent intent)
    {
        var a = Aktif ?? throw new InvalidOperationException("Uygulama penceresi hazır değil.");
        var tcs = new TaskCompletionSource<(Result, Intent?)>();
        int no;
        lock (_bekleyenler) { no = ++_istekNo; _bekleyenler[no] = tcs; }
        a.StartActivityForResult(intent, no);
        return tcs.Task;
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        TaskCompletionSource<(Result, Intent?)>? tcs;
        lock (_bekleyenler) _bekleyenler.Remove(requestCode, out tcs);
        tcs?.TrySetResult((resultCode, data));
    }
}
