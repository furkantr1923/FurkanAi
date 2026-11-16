using FurkanAi.Mobile.Services;

namespace FurkanAi.Mobile;

public partial class App : Application
{
    private readonly UygulamaYasamDongusu _yasam;

    public App(UygulamaYasamDongusu yasam)
    {
        InitializeComponent();
        _yasam = yasam;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var pencere = new Window(new MainPage()) { Title = "FURKAN AI" };
        pencere.Stopped += (_, _) => _yasam.ArkaPlana();
        pencere.Resumed += (_, _) => _yasam.OnPlana();
        return pencere;
    }
}
