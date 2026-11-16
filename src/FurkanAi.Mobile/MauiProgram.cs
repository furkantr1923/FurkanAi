using System.Globalization;
using Microsoft.Extensions.Logging;
using FurkanAi.Mobile.Services;

namespace FurkanAi.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var tr = new CultureInfo("tr-TR");
        CultureInfo.DefaultThreadCurrentCulture = tr;
        CultureInfo.DefaultThreadCurrentUICulture = tr;
        CultureInfo.CurrentCulture = tr;
        CultureInfo.CurrentUICulture = tr;

        HataKaydi.Kur();

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.Services.AddMauiBlazorWebView();
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif
        var s = builder.Services;
        s.AddSingleton<Ayarlar>();
        s.AddSingleton<Arayuz>();
        s.AddSingleton<Veritabani>();
        s.AddSingleton<SohbetDeposu>();
        s.AddSingleton<SaglayiciServisi>();
        s.AddSingleton<SesServisi>();
        s.AddSingleton<UygulamaYasamDongusu>();
        s.AddSingleton<SohbetServisi>();
        s.AddSingleton<KilitServisi>();
        return builder.Build();
    }
}
