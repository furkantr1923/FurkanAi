namespace FurkanAi.Mobile;

public partial class MainPage : ContentPage
{
    public MainPage() => InitializeComponent();

    /// <summary>Android geri tuşu: önce açık alt sayfa/diyalog, sonra sayfa geçmişi.</summary>
    protected override bool OnBackButtonPressed() => Services.GeriTusu.Isle() || base.OnBackButtonPressed();
}
