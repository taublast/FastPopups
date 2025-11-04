using AppoMobi.Maui.FastPopups;

namespace SampleApp.Views;

public partial class FillPopup : Popup
{
    public FillPopup()
    {
        InitializeComponent();
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        Close();
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
    }
}