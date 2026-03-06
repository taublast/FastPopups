using AppoMobi.Maui.FastPopups;
using System.Diagnostics;

namespace SampleApp.Views;

public partial class AnchoredPopup : Popup
{
    public AnchoredPopup()
    {
        InitializeComponent();
    }

    public override void OnOpened()
    {
        base.OnOpened();

        Debug.WriteLine($"DisplayMode is: {DisplayMode}");
    }

    private void OnGotItClicked(object sender, EventArgs e)
    {
        Close();
    }

    public override bool OnBackgroundClicked()
    {

        Trace.WriteLine("OnBackgroundClicked !");

        return base.OnBackgroundClicked();
    }

    private void OnInsetsClicked(object? sender, EventArgs e)
    {
        DisplayMode = DisplayMode switch
        {
            PopupDisplayMode.Default => PopupDisplayMode.Cover,
            PopupDisplayMode.Cover => PopupDisplayMode.FullScreen,
            PopupDisplayMode.FullScreen => PopupDisplayMode.Default,
            _ => PopupDisplayMode.Default
        };
        Debug.WriteLine($"DisplayMode changed to: {DisplayMode}");
    }
}