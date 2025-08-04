
using System.Diagnostics;

namespace SampleApp.Views;

public partial class AnchoredPopup : Popup
{
    public AnchoredPopup()
    {
        InitializeComponent();
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
        IsFullScreen = !IsFullScreen;
    }
}